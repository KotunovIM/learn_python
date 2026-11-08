import sys
import json
import os
import sqlite3
from datetime import datetime

from PySide6.QtCore import Qt, QAbstractTableModel, QModelIndex, Signal
from PySide6.QtGui import QFont, QAction
from PySide6.QtWidgets import (
    QApplication, QMainWindow, QWidget, QTabWidget, QVBoxLayout, QHBoxLayout,
    QListWidget, QListWidgetItem, QPushButton, QLabel, QTableView, QProgressBar,
    QMessageBox, QInputDialog, QSplitter, QFrame, QAbstractItemView, QHeaderView,
    QToolBar, QStyle
)

DATA_FILE = "operations.json"
DB_FILE = "journal.db"


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


# ============================ МОДЕЛИ ТАБЛИЦ ============================
class SqlTableModel(QAbstractTableModel):
    """Простая модель для отображения результата SQL-запроса в QTableView."""
    def __init__(self, headers, rows=None):
        super().__init__()
        self.headers = headers
        self.rows = rows or []

    def set_rows(self, rows):
        self.beginResetModel()
        self.rows = rows
        self.endResetModel()

    def rowCount(self, parent=QModelIndex()):
        return len(self.rows)

    def columnCount(self, parent=QModelIndex()):
        return len(self.headers)

    def data(self, index, role=Qt.DisplayRole):
        if not index.isValid():
            return None
        if role == Qt.DisplayRole:
            return str(self.rows[index.row()][index.column()])
        if role == Qt.TextAlignmentRole:
            return int(Qt.AlignLeft | Qt.AlignVCenter)
        return None

    def headerData(self, section, orientation, role=Qt.DisplayRole):
        if role == Qt.DisplayRole and orientation == Qt.Horizontal:
            return self.headers[section]
        return None


# ============================ ГЛАВНОЕ ОКНО ============================
class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("Чек-лист операций")
        self.resize(1000, 680)

        self.ops = load_operations()
        self.conn = init_db()

        # Состояние выполнения
        self.current_op = None
        self.current_actions = []
        self.current_index = 0
        self.started_at = None

        self._build_ui()
        self._apply_styles()
        self.refresh_ops()
        self.refresh_log()

    # ---------------------- UI ----------------------
    def _build_ui(self):
        # Toolbar
        tb = QToolBar("Главное")
        tb.setMovable(False)
        self.addToolBar(tb)

        act_refresh = QAction(self.style().standardIcon(QStyle.SP_BrowserReload),
                              "Обновить", self)
        act_refresh.triggered.connect(self.refresh_all)
        tb.addAction(act_refresh)

        act_about = QAction(self.style().standardIcon(QStyle.SP_MessageBoxInformation),
                            "О программе", self)
        act_about.triggered.connect(self.show_about)
        tb.addAction(act_about)

        # Tabs
        self.tabs = QTabWidget()
        self.setCentralWidget(self.tabs)

        self.tab_ops = QWidget()
        self.tab_run = QWidget()
        self.tab_log = QWidget()
        self.tabs.addTab(self.tab_ops, "Операции")
        self.tabs.addTab(self.tab_run, "Выполнение")
        self.tabs.addTab(self.tab_log, "Журнал")

        self._build_ops_tab()
        self._build_run_tab()
        self._build_log_tab()

    def _build_ops_tab(self):
        layout = QHBoxLayout(self.tab_ops)

        splitter = QSplitter(Qt.Horizontal)
        layout.addWidget(splitter)

        # Левая панель — операции
        left = QWidget()
        ll = QVBoxLayout(left)
        ll.setContentsMargins(0, 0, 0, 0)

        header_l = QLabel("Операции")
        header_l.setObjectName("sectionHeader")
        ll.addWidget(header_l)

        self.list_ops = QListWidget()
        self.list_ops.currentItemChanged.connect(lambda *_: self.refresh_actions())
        ll.addWidget(self.list_ops, 1)

        row1 = QHBoxLayout()
        btn_add_op = QPushButton("+ Операция")
        btn_add_op.clicked.connect(self.add_operation)
        btn_del_op = QPushButton("− Удалить")
        btn_del_op.clicked.connect(self.del_operation)
        btn_run_op = QPushButton("▶ Выполнить")
        btn_run_op.setObjectName("primaryBtn")
        btn_run_op.clicked.connect(self.start_operation)
        row1.addWidget(btn_add_op)
        row1.addWidget(btn_del_op)
        row1.addWidget(btn_run_op)
        ll.addLayout(row1)

        # Правая панель — действия
        right = QWidget()
        rl = QVBoxLayout(right)
        rl.setContentsMargins(0, 0, 0, 0)

        header_r = QLabel("Действия выбранной операции")
        header_r.setObjectName("sectionHeader")
        rl.addWidget(header_r)

        self.list_actions = QListWidget()
        rl.addWidget(self.list_actions, 1)

        row2 = QHBoxLayout()
        btn_add_act = QPushButton("+ Действие")
        btn_add_act.clicked.connect(self.add_action)
        btn_del_act = QPushButton("− Действие")
        btn_del_act.clicked.connect(self.del_action)
        row2.addWidget(btn_add_act)
        row2.addWidget(btn_del_act)
        row2.addStretch(1)
        rl.addLayout(row2)

        splitter.addWidget(left)
        splitter.addWidget(right)
        splitter.setSizes([380, 600])

    def _build_run_tab(self):
        v = QVBoxLayout(self.tab_run)
        v.setContentsMargins(24, 24, 24, 24)

        self.lbl_run_title = QLabel("Операция не выбрана")
        self.lbl_run_title.setObjectName("runTitle")
        self.lbl_run_title.setAlignment(Qt.AlignCenter)
        v.addWidget(self.lbl_run_title)

        self.lbl_step = QLabel("")
        self.lbl_step.setAlignment(Qt.AlignCenter)
        self.lbl_step.setObjectName("stepLabel")
        v.addWidget(self.lbl_step)

        self.progress = QProgressBar()
        self.progress.setMinimum(0)
        self.progress.setMaximum(1)
        self.progress.setValue(0)
        self.progress.setTextVisible(True)
        v.addWidget(self.progress)

        # Карточка действия
        card = QFrame()
        card.setObjectName("actionCard")
        cl = QVBoxLayout(card)
        self.lbl_action = QLabel("—")
        self.lbl_action.setObjectName("actionText")
        self.lbl_action.setAlignment(Qt.AlignCenter)
        self.lbl_action.setWordWrap(True)
        cl.addWidget(self.lbl_action)
        v.addWidget(card, 1)

        btns = QHBoxLayout()
        btns.addStretch(1)
        self.btn_confirm = QPushButton("✔ Подтвердить")
        self.btn_confirm.setObjectName("primaryBtn")
        self.btn_confirm.clicked.connect(self.confirm_step)
        self.btn_confirm.setEnabled(False)
        btns.addWidget(self.btn_confirm)

        self.btn_abort = QPushButton("✖ Прервать")
        self.btn_abort.setObjectName("dangerBtn")
        self.btn_abort.clicked.connect(self.abort_run)
        self.btn_abort.setEnabled(False)
        btns.addWidget(self.btn_abort)
        btns.addStretch(1)
        v.addLayout(btns)

        self.lbl_status = QLabel("")
        self.lbl_status.setAlignment(Qt.AlignCenter)
        self.lbl_status.setObjectName("statusLabel")
        v.addWidget(self.lbl_status)

    def _build_log_tab(self):
        v = QVBoxLayout(self.tab_log)

        top = QHBoxLayout()
        btn_refresh = QPushButton("🔄 Обновить")
        btn_refresh.clicked.connect(self.refresh_log)
        top.addWidget(btn_refresh)
        top.addStretch(1)
        v.addLayout(top)

        lbl1 = QLabel("Подтверждения действий (последние 100)")
        lbl1.setObjectName("sectionHeader")
        v.addWidget(lbl1)

        self.model_actions = SqlTableModel(["Время", "Операция", "Действие"])
        self.view_actions = QTableView()
        self.view_actions.setModel(self.model_actions)
        self._setup_table(self.view_actions)
        v.addWidget(self.view_actions, 3)

        lbl2 = QLabel("Завершённые операции (последние 50)")
        lbl2.setObjectName("sectionHeader")
        v.addWidget(lbl2)

        self.model_ops = SqlTableModel(["Завершено", "Операция", "Начато"])
        self.view_ops = QTableView()
        self.view_ops.setModel(self.model_ops)
        self._setup_table(self.view_ops)
        v.addWidget(self.view_ops, 2)

    @staticmethod
    def _setup_table(view):
        view.setSelectionBehavior(QAbstractItemView.SelectRows)
        view.setEditTriggers(QAbstractItemView.NoEditTriggers)
        view.setAlternatingRowColors(True)
        view.verticalHeader().setVisible(False)
        view.horizontalHeader().setStretchLastSection(True)
        view.horizontalHeader().setSectionResizeMode(QHeaderView.Stretch)

    def _apply_styles(self):
        self.setStyleSheet("""
            QMainWindow, QWidget { background: #f5f6fa; }
            QTabWidget::pane { border: 1px solid #dcdde1; border-radius: 6px; }
            QTabBar::tab {
                padding: 8px 18px; background: #ecf0f1;
                border-top-left-radius: 6px; border-top-right-radius: 6px;
                margin-right: 2px;
            }
            QTabBar::tab:selected { background: #ffffff; font-weight: bold; }

            QLabel#sectionHeader {
                font-size: 13px; font-weight: bold; color: #2f3640;
                padding: 4px 0;
            }
            QLabel#runTitle { font-size: 20px; font-weight: bold; color: #2f3640; }
            QLabel#stepLabel { color: #718093; }
            QLabel#statusLabel { color: #009432; font-weight: bold; }

            QFrame#actionCard {
                background: #ffffff; border: 1px solid #dcdde1;
                border-radius: 10px;
            }
            QLabel#actionText { font-size: 26px; color: #2f3640; }

            QPushButton {
                padding: 6px 12px; border-radius: 6px;
                border: 1px solid #dcdde1; background: #ffffff;
            }
            QPushButton:hover { background: #f1f2f6; }
            QPushButton:disabled { color: #b2bec3; }

            QPushButton#primaryBtn {
                background: #1e90ff; color: white; border: none;
                font-weight: bold;
            }
            QPushButton#primaryBtn:hover { background: #1873d1; }
            QPushButton#primaryBtn:disabled { background: #b2bec3; }

            QPushButton#dangerBtn {
                background: #e84118; color: white; border: none;
                font-weight: bold;
            }
            QPushButton#dangerBtn:hover { background: #c23616; }
            QPushButton#dangerBtn:disabled { background: #b2bec3; }

            QListWidget {
                background: white; border: 1px solid #dcdde1;
                border-radius: 6px; padding: 4px;
            }
            QListWidget::item { padding: 4px; }
            QListWidget::item:selected { background: #dff9fb; color: #2f3640; }

            QTableView {
                background: white; border: 1px solid #dcdde1;
                border-radius: 6px; gridline-color: #ecf0f1;
            }
            QHeaderView::section {
                background: #f1f2f6; padding: 4px; border: none;
                border-bottom: 1px solid #dcdde1; font-weight: bold;
            }

            QProgressBar {
                border: 1px solid #dcdde1; border-radius: 6px;
                background: white; text-align: center; height: 20px;
            }
            QProgressBar::chunk {
                background: #1e90ff; border-radius: 5px;
            }
        """)

    # ---------------------- Логика операций ----------------------
    def refresh_all(self):
        self.ops = load_operations()
        self.refresh_ops()
        self.refresh_log()

    def refresh_ops(self):
        self.list_ops.clear()
        for name in self.ops:
            item = QListWidgetItem(name)
            self.list_ops.addItem(item)
        if self.list_ops.count():
            self.list_ops.setCurrentRow(0)
        self.refresh_actions()

    def refresh_actions(self):
        self.list_actions.clear()
        name = self.selected_operation()
        if not name:
            return
        for i, a in enumerate(self.ops[name], 1):
            self.list_actions.addItem(f"{i}. {a}")

    def selected_operation(self):
        item = self.list_ops.currentItem()
        return item.text() if item else None

    def add_operation(self):
        name, ok = QInputDialog.getText(self, "Новая операция", "Название:")
        if not ok or not name.strip():
            return
        name = name.strip()
        if name in self.ops:
            QMessageBox.warning(self, "Внимание", "Такая операция уже есть.")
            return
        self.ops[name] = []
        save_operations(self.ops)
        self.refresh_ops()

    def del_operation(self):
        name = self.selected_operation()
        if not name:
            return
        if QMessageBox.question(self, "Удалить", f"Удалить операцию «{name}»?") != QMessageBox.Yes:
            return
        del self.ops[name]
        save_operations(self.ops)
        self.refresh_ops()

    def add_action(self):
        name = self.selected_operation()
        if not name:
            QMessageBox.information(self, "Внимание", "Сначала выберите операцию.")
            return
        action, ok = QInputDialog.getText(self, "Новое действие", "Описание:")
        if ok and action.strip():
            self.ops[name].append(action.strip())
            save_operations(self.ops)
            self.refresh_actions()

    def del_action(self):
        name = self.selected_operation()
        idx = self.list_actions.currentRow()
        if name is None or idx < 0:
            return
        del self.ops[name][idx]
        save_operations(self.ops)
        self.refresh_actions()

    # ---------------------- Выполнение ----------------------
    def start_operation(self):
        name = self.selected_operation()
        if not name:
            QMessageBox.information(self, "Внимание", "Выберите операцию.")
            return
        if not self.ops[name]:
            QMessageBox.information(self, "Внимание", "У операции нет действий.")
            return
        self.current_op = name
        self.current_actions = list(self.ops[name])
        self.current_index = 0
        self.started_at = datetime.now().strftime("%Y-%m-%d %H:%M:%S")

        self.tabs.setCurrentWidget(self.tab_run)
        self.lbl_run_title.setText(f"Операция: {name}")
        self.progress.setMaximum(len(self.current_actions))
        self.progress.setValue(0)
        self.lbl_status.setText(f"Начато: {self.started_at}")
        self.btn_confirm.setEnabled(True)
        self.btn_abort.setEnabled(True)
        self.show_step()

    def show_step(self):
        if self.current_index >= len(self.current_actions):
            self.finish_operation()
            return
        action = self.current_actions[self.current_index]
        self.lbl_step.setText(f"Шаг {self.current_index + 1} из {len(self.current_actions)}")
        self.lbl_action.setText(action)
        self.progress.setValue(self.current_index)

    def confirm_step(self):
        action = self.current_actions[self.current_index]
        ts = log_action(self.conn, self.current_op, action)
        self.lbl_status.setText(f"✔ «{action}» — {ts}")
        self.current_index += 1
        self.progress.setValue(self.current_index)
        self.show_step()

    def finish_operation(self):
        finished = log_operation(self.conn, self.current_op, self.started_at)
        self.lbl_action.setText(f"✅ Операция «{self.current_op}» завершена")
        self.lbl_step.setText("")
        self.progress.setValue(len(self.current_actions))
        self.lbl_status.setText(f"Завершено: {finished}")
        self.btn_confirm.setEnabled(False)
        self.btn_abort.setEnabled(False)
        self.refresh_log()

    def abort_run(self):
        if QMessageBox.question(self, "Прервать",
                                "Прервать выполнение? Завершение не будет зафиксировано.") != QMessageBox.Yes:
            return
        self.lbl_action.setText(f"Операция «{self.current_op}» прервана")
        self.lbl_step.setText("")
        self.lbl_status.setText("Прервано пользователем.")
        self.btn_confirm.setEnabled(False)
        self.btn_abort.setEnabled(False)
        self.refresh_log()

    # ---------------------- Журнал ----------------------
    def refresh_log(self):
        rows1 = self.conn.execute(
            "SELECT operation, action, confirmed_at FROM action_log ORDER BY id DESC LIMIT 100"
        ).fetchall()
        self.model_actions.set_rows([(r[2], r[0], r[1]) for r in rows1])

        rows2 = self.conn.execute(
            "SELECT operation, started_at, finished_at FROM operation_log ORDER BY id DESC LIMIT 50"
        ).fetchall()
        self.model_ops.set_rows([(r[2], r[0], r[1]) for r in rows2])

    def show_about(self):
        QMessageBox.about(self, "О программе",
                          "Чек-лист операций\n\nPySide6 + SQLite + JSON\n"
                          "Журнал подтверждений действий и завершённых операций.")


# ============================ ЗАПУСК ============================
if __name__ == "__main__":
    app = QApplication(sys.argv)
    app.setFont(QFont("Segoe UI", 10))
    w = MainWindow()
    w.show()
    sys.exit(app.exec())