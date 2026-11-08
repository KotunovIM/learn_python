
python
BASE_DIR = "/storage/emulated/0/Download/checklist_app"
Клавиатура: в Pydroid всплывающая клавиатура иногда перекрывает TextInput в Popup. Решение — в TextInputPopup добавьте:

python
self.input.bind(focus=lambda w, v: setattr(self, "pos_hint", {"top": 1}) if v else None)
Back-кнопка: по умолчанию Kivy выходит из приложения. Если хотите, чтобы она возвращала на экран «Операции», добавьте в ChecklistApp:
def on_start(self):
    from kivy.core.window import Window
    Window.bind(on_keyboard=self._on_key)
    Clock.schedule_once(lambda *_: self._prime_log(), 0)

def _on_key(self, window, key, *args):
    if key == 27:  # Android Back
        sm = self.root
        if sm.current != "ops":
            sm.current = "ops"
            return True
    return False