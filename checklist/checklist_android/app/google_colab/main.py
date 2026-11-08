class OperationsScreen(Screen):
    selected_op = StringProperty("")

    def __init__(self, **kwargs):
        super().__init__(**kwargs)
        self.app = App.get_running_app()

    def refresh(self):
        self.app.ops = load_operations()
        self.rebuild_ops_list()
        if self.app.ops and (not self.selected_op or self.selected_op not in self.app.ops):
            self.selected_op = list(self.app.ops)[0]
        self.refresh_actions()

    def rebuild_ops_list(self):
        box = self.ids.ops_box
        box.clear_widgets()
        for name in self.app.ops:
            btn = Button(
                text=name,
                size_hint_y=None,
                height=40,
                background_color=(0.12, 0.56, 1, 1) if name == self.selected_op else (0.9, 0.9, 0.9, 1),
                color=(1, 1, 1, 1) if name == self.selected_op else (0.2, 0.2, 0.2, 1)
            )
            btn.bind(on_release=lambda b, n=name: self.on_operation_selected(n))
            box.add_widget(btn)

    def refresh_actions(self):
        box = self.ids.actions_box
        box.clear_widgets()
        if not self.selected_op or self.selected_op not in self.app.ops:
            return
        for i, a in enumerate(self.app.ops[self.selected_op], 1):
            lbl = Label(
                text=f"{i}. {a}",
                size_hint_y=None,
                height=36,
                halign="left",
                valign="middle",
                text_size=(self.width - 20, None),
                color=(0.15, 0.15, 0.15, 1)
            )
            lbl.bind(width=lambda w, v, l=lbl: setattr(l, "text_size", (v - 20, None)))
            box.add_widget(lbl)

    def on_operation_selected(self, name):
        self.selected_op = name
        self.rebuild_ops_list()
        self.refresh_actions()

    # --- кнопки (без изменений) ---
    def add_operation(self):
        TextInputPopup(title="Новая операция", on_ok=self._create_operation).open()

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
        ConfirmPopup(f"Удалить операцию «{name}»?",
                     on_yes=lambda: self._delete_operation(name)).open()

    def _delete_operation(self, name):
        self.app.ops.pop(name, None)
        save_operations(self.app.ops)
        self.selected_op = ""
        self.refresh()

    def add_action(self):
        if not self.selected_op:
            InfoPopup("Сначала выберите операцию.").open()
            return
        TextInputPopup(title="Новое действие", on_ok=self._create_action).open()

    def _create_action(self, text):
        text = text.strip()
        if not text:
            return
        self.app.ops[self.selected_op].append(text)
        save_operations(self.app.ops)
        self.refresh_actions()

    def del_action(self):
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


class LogScreen(Screen):
    def refresh(self):
        conn = App.get_running_app().conn

        box1 = self.ids.log_actions_box
        box1.clear_widgets()
        rows = conn.execute(
            "SELECT operation, action, confirmed_at FROM action_log ORDER BY id DESC LIMIT 100"
        ).fetchall()
        if not rows:
            box1.add_widget(Label(text="Пока пусто", size_hint_y=None, height=30,
                                  color=(0.5, 0.5, 0.5, 1)))
        for op, act, ts in rows:
            lbl = Label(
                text=f"[{ts}]  {op} → {act}",
                size_hint_y=None,
                height=36,
                halign="left",
                valign="middle",
                text_size=(self.width - 20, None),
                color=(0.15, 0.15, 0.15, 1)
            )
            lbl.bind(width=lambda w, v, l=lbl: setattr(l, "text_size", (v - 20, None)))
            box1.add_widget(lbl)

        box2 = self.ids.log_ops_box
        box2.clear_widgets()
        rows = conn.execute(
            "SELECT operation, started_at, finished_at FROM operation_log ORDER BY id DESC LIMIT 50"
        ).fetchall()
        if not rows:
            box2.add_widget(Label(text="Пока пусто", size_hint_y=None, height=30,
                                  color=(0.5, 0.5, 0.5, 1)))
        for op, st, fin in rows:
            lbl = Label(
                text=f"[{fin}]  {op}  (начата: {st})",
                size_hint_y=None,
                height=36,
                halign="left",
                valign="middle",
                text_size=(self.width - 20, None),
                color=(0.15, 0.15, 0.15, 1)
            )
            lbl.bind(width=lambda w, v, l=lbl: setattr(l, "text_size", (v - 20, None)))
            box2.add_widget(lbl)

    def go_back(self):
        self.manager.current = "ops"