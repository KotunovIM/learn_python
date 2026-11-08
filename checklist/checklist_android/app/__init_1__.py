def __init__(self, **kwargs):
    super().__init__(**kwargs)
    self.bind(action_rows=lambda *_: setattr(
        self.ids.rv_actions if "rv_actions" in self.ids else self.ids.get("rv_ops"),
        "data", self.action_rows))