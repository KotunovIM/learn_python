import json
import os
import sqlite3
from datetime import datetime

from kivy.app import App
from kivy.clock import Clock
from kivy.properties import StringProperty, NumericProperty, ListProperty, BooleanProperty
from kivy.uix.screenmanager import ScreenManager, Screen
from kivy.uix.popup import Popup
from kivy.uix.boxlayout import BoxLayout
from kivy.uix.label import Label
from kivy.uix.textinput import TextInput
from kivy.uix.button import Button
from kivy.uix.recycleview import RecycleView
from kivy.uix.recycleview.views import RecycleDataViewBehavior
from kivy.uix.behaviors import ButtonBehavior

# Пути — на Android нужно писать в user_data_dir
from kivy.utils import platform
if platform == "android":
    from android.storage import app_storage_path  # noqa
    BASE_DIR = app_storage_path()
else:
    BASE_DIR = os.path.dirname(os.path.abspath(__file__))

DATA_FILE = os.path.join(BASE_DIR, "operations.json")
DB_FILE = os.path.join(BASE_DIR, "journal.db")


# ============================ ХРАНИЛИЩЕ ============================
def load_operations():
    if not os.path.exists(DATA_FILE):
        default = {
            "Выхожу из дома": ["Выключил газ", "Выключил свет", "Перекрыл воду",
                               "Закрыл окна", "Закрыл дверь"],
            "Ложусь спать": ["Выключил технику", "Поставил будильник", "Погасил свет"]
        }
        save_operations(default)
        return default
    with open(DATA_FILE, "r", encoding="utf-8") as f:
        return json.load(f)


def save_operations(ops):
    with open(DATA_FILE, "w", encoding="utf-8") as f:
        json.dump(ops, f, ensure_ascii=False, indent=2)


def init_db():
    conn = sqlite3.connect(DB_FILE)
    cur = conn.cursor()
    cur.execute("""
        CREATE TABLE IF NOT EXISTS action_log (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            operation TEXT NOT NULL,
            action TEXT NOT NULL,
            confirmed_at TEXT NOT NULL
        )
    """)
    cur.execute("""
        CREATE TABLE IF NOT EXISTS operation_log (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            operation TEXT NOT NULL,
            started_at TEXT NOT NULL,
            finished_at TEXT NOT NULL
        )
    """)
    conn.commit()
    return conn


def log_action(conn, operation, action):
    ts = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    conn.execute(
        "INSERT INTO action_log (operation, action, confirmed_at) VALUES (?, ?, ?)",
        (operation, action, ts)
    )
    conn.commit()
    return ts


def log_operation(conn, operation, started_at):
    finished = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    conn.execute(
        "INSERT INTO operation_log (operation, started_at, finished_at) VALUES (?, ?, ?)",
        (operation, started_at, finished)
    )
    conn.commit()
    return finished


# ============================ ВИДЖЕТЫ ============================
class ClickableLabel(RecycleDataViewBehavior, ButtonBehavior, Label):
    """Элемент списка операций (в RecycleView)."""
    index = None

    def refresh_view_attrs(self, rv, index, data):
        self.index = index
        self.text = data.get("text", "")
        return super().refresh_view_attrs(rv, index, data)

    def on_release(self):
        app = App.get_running_app()
        app.select_operation_by_index(self.index)


class ActionLabel(Label):
    """Элемент списка действий."""
    pass


# ============================ ЭКРАНЫ ============================
class OperationsScreen(Screen):
    op_titles = ListProperty([])
    action_lines = ListProperty([])
    selected_op = StringProperty("")

    def __init__(self, **kwargs):
        super().__init__(**kwargs)
        self.app = App.get_running_app()

    def refresh(self):
        self.app.ops = load_operations()
        self.op_titles = [{"text": name} for name in self.app.ops]

        # Автовыбор первой операции
        if self.app.ops and not self.selected_op:
            self.selected_op = list(self.app.ops)[0]
        self.refresh_actions()

    def refresh_actions(self):
        self.action_lines = []
        if self.selected_op and self.selected_op in self.app.ops:
            for i, a in enumerate(self.app.ops[self.selected_op], 1):
                self.action_lines.append(f"{i}. {a}")

    def on_operation_selected(self, name):
        self.selected_op = name
        self.refresh_actions()

    # --- Кнопки ---
    def add_operation(self):
        popup = TextInputPopup(title="Новая операция",
                               on_ok=self._create_operation)
        popup.open()

    def _create_operation(self, name):
        name = name.strip()
        if not name:
            return
        if name in self.app.ops:
            InfoPopup("Такая операция уже есть.").open()
            return
        self.app.ops[name] = []
        save_operations(self.app.ops)
        self.selected_op = name
        self.refresh()

    def del_operation(self):
        if not self.selected_op:
            return
        name = self.selected_op
        ConfirmPopup(
            f"Удалить операцию «{name}»?",
            on_yes=lambda: self._delete_operation(name)
        ).open()

    def _delete_operation(self, name):
        self.app.ops.pop(name, None)
        save_operations(self.app.ops)
        self.selected_op = ""
        self.refresh()

    def add_action(self):
        if not self.selected_op:
            InfoPopup("Сначала выберите операцию.").open()
            return
        TextInputPopup(title="Новое действие",
                       on_ok=self._create_action).open()

    def _create_action(self, text):
        text = text.strip()
        if not text:
            return
        self.app.ops[self.selected_op].append(text)
        save_operations(self.app.ops)
        self.refresh_actions()

    def del_action(self):
        # Удаляем последнее действие выбранной операции (для простоты)
        if not self.selected_op or not self.app.ops[self.selected_op]:
            return
        self.app.ops[self.selected_op].pop()
        save_operations(self.app.ops)
        self.refresh_actions()

    def run_operation(self):
        if not self.selected_op:
            InfoPopup("Выберите операцию.").open()
            return
        if not self.app.ops[self.selected_op]:
            InfoPopup("У операции нет действий.").open()
            return
        run = self.manager.get_screen("run")
        run.start(self.selected_op, list(self.app.ops[self.selected_op]))
        self.manager.current = "run"


class RunScreen(Screen):
    op_name = StringProperty("")
    step_text = StringProperty("")
    action_text = StringProperty("—")
    status_text = StringProperty("")
    progress_value = NumericProperty(0)
    progress_max = NumericProperty(1)
    controls_enabled = BooleanProperty(False)

    def __init__(self, **kwargs):
        super().__init__(**kwargs)
        self.app = App.get_running_app()
        self.actions = []
        self.index = 0
        self.started_at = None

    def start(self, op_name, actions):
        self.op_name = op_name
        self.actions = actions
        self.index = 0
        self.started_at = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        self.progress_max = len(actions)
        self.progress_value = 0
        self.status_text = f"Начато: {self.started_at}"
        self.controls_enabled = True
        self._show_step()

    def _show_step(self):
        if self.index >= len(self.actions):
            self._finish()
            return
        self.step_text = f"Шаг {self.index + 1} из {len(self.actions)}"
        self.action_text = self.actions[self.index]

    def confirm(self):
        if not self.controls_enabled:
            return
        action = self.actions[self.index]
        ts = log_action(self.app.conn, self.op_name, action)
        self.status_text = f"✔ «{action}» — {ts}"
        self.index += 1
        self.progress_value = self.index
        # Небольшая задержка, чтобы пользователь увидел статус
        Clock.schedule_once(lambda *_: self._show_step(), 0.3)

    def _finish(self):
        finished = log_operation(self.app.conn, self.op_name, self.started_at)
        self.action_text = f"✅ Операция «{self.op_name}» завершена"
        self.step_text = ""
        self.progress_value = len(self.actions)
        self.status_text = f"Завершено: {finished}"
        self.controls_enabled = False

    def abort(self):
        if not self.controls_enabled:
            return
        ConfirmPopup("Прервать выполнение?\nЗавершение не будет зафиксировано.",
                     on_yes=self._do_abort).open()

    def _do_abort(self):
        self.action_text = f"Операция «{self.op_name}» прервана"
        self.step_text = ""
        self.status_text = "Прервано пользователем."
        self.controls_enabled = False


class LogScreen(Screen):
    action_rows = ListProperty([])
    op_rows = ListProperty([])

    def refresh(self):
        conn = App.get_running_app().conn

        rows = conn.execute(
            "SELECT operation, action, confirmed_at FROM action_log ORDER BY id DESC LIMIT 100"
        ).fetchall()
        self.action_rows = [
            {"text": f"[{ts}]  {op}\n    → {act}"} for op, act, ts in rows
        ]

        rows = conn.execute(
            "SELECT operation, started_at, finished_at FROM operation_log ORDER BY id DESC LIMIT 50"
        ).fetchall()
        self.op_rows = [
            {"text": f"[{fin}]  {op}\n    начата: {st}"} for op, st, fin in rows
        ]

    def go_back(self):
        self.manager.current = "ops"


# ============================ POPUP-Ы ============================
class TextInputPopup(Popup):
    def __init__(self, title="Ввод", on_ok=None, **kwargs):
        super().__init__(title=title, size_hint=(0.9, 0.4), **kwargs)
        self._on_ok = on_ok
        layout = BoxLayout(orientation="vertical", padding=10, spacing=10)
        self.input = TextInput(multiline=False)
        layout.add_widget(self.input)
        row = BoxLayout(size_hint_y=None, height=44, spacing=8)
        cancel = Button(text="Отмена", on_release=lambda *_: self.dismiss())
        ok = Button(text="OK", on_release=self._submit)
        row.add_widget(cancel)
        row.add_widget(ok)
        layout.add_widget(row)
        self.content = layout

    def _submit(self, *args):
        text = self.input.text
        self.dismiss()
        if self._on_ok:
            self._on_ok(text)


class ConfirmPopup(Popup):
    def __init__(self, text, on_yes=None, **kwargs):
        super().__init__(title="Подтверждение", size_hint=(0.9, 0.35), **kwargs)
        self._on_yes = on_yes
        layout = BoxLayout(orientation="vertical", padding=10, spacing=10)
        layout.add_widget(Label(text=text, halign="center"))
        row = BoxLayout(size_hint_y=None, height=44, spacing=8)
        row.add_widget(Button(text="Нет", on_release=lambda *_: self.dismiss()))
        row.add_widget(Button(text="Да", on_release=self._yes))
        layout.add_widget(row)
        self.content = layout

    def _yes(self, *args):
        self.dismiss()
        if self._on_yes:
            self._on_yes()


class InfoPopup(Popup):
    def __init__(self, text, **kwargs):
        super().__init__(title="Информация", size_hint=(0.9, 0.3), **kwargs)
        layout = BoxLayout(orientation="vertical", padding=10, spacing=10)
        layout.add_widget(Label(text=text, halign="center"))
        layout.add_widget(Button(text="OK", size_hint_y=None, height=44,
                                 on_release=lambda *_: self.dismiss()))
        self.content = layout


# ============================ ПРИЛОЖЕНИЕ ============================
class ChecklistApp(App):
    def build(self):
        self.title = "Чек-лист операций"
        self.ops = load_operations()
        self.conn = init_db()

        sm = ScreenManager()
        ops_screen = OperationsScreen(name="ops")
        run_screen = RunScreen(name="run")
        log_screen = LogScreen(name="log")
        sm.add_widget(ops_screen)
        sm.add_widget(run_screen)
        sm.add_widget(log_screen)

        Clock.schedule_once(lambda *_: ops_screen.refresh(), 0)
        return sm

    def on_start(self):
        # Подгружаем журнал при первом показе
        Clock.schedule_once(lambda *_: self._prime_log(), 0)

    def _prime_log(self):
        log = self.root.get_screen("log")
        log.refresh()

    def select_operation_by_index(self, idx):
        ops_screen = self.root.get_screen("ops")
        names = list(self.ops.keys())
        if 0 <= idx < len(names):
            ops_screen.on_operation_selected(names[idx])

    def on_tab_log(self):
        self.root.get_screen("log").refresh()
        self.root.current = "log"

    def on_tab_ops(self):
        self.root.get_screen("ops").refresh()
        self.root.current = "ops"


if __name__ == "__main__":
    ChecklistApp().run()