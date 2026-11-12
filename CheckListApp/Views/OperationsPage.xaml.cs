using System.Collections.ObjectModel;

namespace CheckListApp.Views;

public partial class OperationsPage : ContentPage
{
    private ObservableCollection<OperationVm> _opsVm = new();
    private OperationVm? _selected;

    public OperationsPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Reload();
    }

    private void Reload()
    {
        var selectedId = _selected?.Id;
        _opsVm = new ObservableCollection<OperationVm>(Storage.LoadOperations());
        CvOperations.ItemsSource = _opsVm;
        _selected = selectedId.HasValue ? _opsVm.FirstOrDefault(o => o.Id == selectedId.Value) : _opsVm.FirstOrDefault();
        if (_selected != null)
        {
            CvOperations.SelectedItem = _selected;
            ShowActions();
        }
        else
        {
            CvActions.ItemsSource = null;
            LblActionsHeader.Text = "Действия операции";
        }
    }

    private void ShowActions()
    {
        if (_selected == null) return;
        LblActionsHeader.Text = $"Действия операции «{_selected.Name}»";
        CvActions.ItemsSource = _selected.Actions;
    }

    private void CvOperations_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selected = e.CurrentSelection.FirstOrDefault() as OperationVm;
        ShowActions();
    }

    private async void AddOperation_Click(object sender, EventArgs e)
    {
        var name = await DisplayPromptAsync("Новая операция", "Название:");
        if (string.IsNullOrWhiteSpace(name)) return;
        name = name.Trim();
        if (Storage.OperationNameExists(name))
        {
            await DisplayAlert("Внимание", "Такая операция уже есть.", "OK");
            return;
        }
        Storage.CreateOperation(name);
        Reload();
    }

    private async void DelOperation_Click(object sender, EventArgs e)
    {
        if (_selected == null) return;
        bool ok = await DisplayAlert("Удаление",
            $"Удалить операцию «{_selected.Name}» и все её действия?",
            "Да", "Нет");
        if (!ok) return;
        Storage.DeleteOperation(_selected.Id);
        _selected = null;
        Reload();
    }

    private async void AddAction_Click(object sender, EventArgs e)
    {
        if (_selected == null) { await DisplayAlert("Внимание", "Сначала выберите операцию.", "OK");
            return; }
        var text = await DisplayPromptAsync("Новое действие", "Описание:");
        if (string.IsNullOrWhiteSpace(text)) return;
        text = text.Trim();
        Storage.AddAction(_selected.Id, text);
        _selected.Actions.Add(text);
    }

    private async void DelAction_Click(object sender, EventArgs e)
    {
        if (_selected == null || _selected.Actions.Count == 0) return;
        bool ok = await DisplayAlert("Удаление",
            $"Удалить последнее действие «{_selected.Actions[^1]}»?",
            "Да", "Нет");        
		if (!ok) return;
        Storage.DeleteLastAction(_selected.Id);
        _selected.Actions.RemoveAt(_selected.Actions.Count - 1);
    }

        private async void OpenLog_Click(object sender, EventArgs e)
        => await Navigation.PushAsync(new LogPage());

    private async void RunOperation_Click(object sender, EventArgs e)
    {
        if (_selected == null)        
		{
            await DisplayAlert("Внимание", "Выберите операцию.", "OK");
            return;
        }

        if (_selected.Actions.Count == 0)         
		{
            await DisplayAlert("Внимание", "У операции нет действий.", "OK");
            return;
        }
        var run = new RunPage();
        run.Start(_selected.Name, _selected.Actions.ToList());
        await Navigation.PushAsync(run);
    }
}