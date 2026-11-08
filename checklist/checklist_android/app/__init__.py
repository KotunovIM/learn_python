def __init__(self, **kwargs):
    super().__init__(**kwargs)
    self.bind(op_titles=lambda *_: setattr(
        self.ids.rv_ops, "data", self.op_titles))