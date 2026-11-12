namespace CheckListApp.Views;

public partial class RunPage : ContentPage
{
    private string _opName = "";
    private List<string> _actions = new();
    private int _index;
    private string _startedAt = "";

    public RunPage() { InitializeComponent(); }

    public void Start(string opName, List<string> actions)
    {
        _opName = opName;
        _actions = actions;
        _index = 0;
        _startedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        LblTitle.Text = $"Операция: {_opName}";
        Progress.Progress = 0;
        LblStatus.Text = $"Начато: {_startedAt}";
        BtnConfirm.IsEnabled = true;
        BtnAbort.IsEnabled = true;
        ShowStep();
    }

    private void ShowStep()
    {
        if (_index >= _actions.Count) { FinishOperation(); return; }
        LblStep.Text = $"Шаг {_index + 1} из {_actions.Count}";
        LblAction.Text = _actions[_index];
        Progress.Progress = (double)_index / _actions.Count;
    }

    private void Confirm_Click(object sender, EventArgs e)
    {
        if (_index >= _actions.Count) return;
        var action = _actions[_index];
        var ts = Storage.LogAction(_opName, action);
        LblStatus.Text = $"✔ «{action}» — {ts}";
        _index++;
        Progress.Progress = (double)_index / _actions.Count;
        ShowStep();
    }

    private void FinishOperation()
    {
        var fin = Storage.LogOperation(_opName, _startedAt);
        LblAction.Text = $"✅ Операция «{_opName}» завершена";
        LblStep.Text = "";
        Progress.Progress = 1;
        LblStatus.Text = $"Завершено: {fin}";
        BtnConfirm.IsEnabled = false;
        BtnAbort.IsEnabled = false;
    }

    private async void Abort_Click(object sender, EventArgs e)
    {
        bool ok = await DisplayAlert("Прервать", "Прервать выполнение?", "Да", "Нет");
        if (!ok) return;
        LblAction.Text = $"Операция «{_opName}» прервана";
        LblStep.Text = "";
        LblStatus.Text = "Прервано пользователем.";
        BtnConfirm.IsEnabled = false;
        BtnAbort.IsEnabled = false;
    }
}