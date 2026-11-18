using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using AndroidApp.Models;
using AndroidApp.Services;

namespace AndroidApp.PageModels;

public class MainPageModel : INotifyPropertyChanged
{
    private string _selectedFilter = "Шаблонные";
    private bool _initialized;

    public ObservableCollection<Checklist>      Templates { get; } = new();
    public ObservableCollection<ArchivedAction> Archive   { get; } = new();

    public ObservableCollection<string> FilterOptions { get; } = new()
    {
        "Шаблонные", "Архив действий"
    };

    public string SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (_selectedFilter == value) return;
            _selectedFilter = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsTemplatesView));
            OnPropertyChanged(nameof(IsArchiveView));
            OnPropertyChanged(nameof(HeaderTitle));
            RefreshView();
        }
    }

    public bool IsTemplatesView => SelectedFilter == "Шаблонные";
    public bool IsArchiveView   => SelectedFilter == "Архив действий";
    public string HeaderTitle   => IsTemplatesView ? "My Checklists" : "Архив действий";

    public ICommand AddChecklistCommand    { get; }
    public ICommand EditChecklistCommand   { get; }
    public ICommand DeleteChecklistCommand { get; }
    public ICommand OpenChecklistCommand   { get; }
    public ICommand RestoreActionCommand   { get; }
    public ICommand DeleteArchiveCommand   { get; }
    public ICommand ClearArchiveCommand    { get; }

    public MainPageModel()
    {
        AddChecklistCommand = new Command(async () =>
            await Shell.Current.GoToAsync("checklist-edit"));

        EditChecklistCommand = new Command<Checklist>(async c =>
        {
            if (c is null) return;
            await Shell.Current.GoToAsync($"checklist-edit?id={c.Id}");
        });

        OpenChecklistCommand = new Command<Checklist>(async c =>
        {
            if (c is null) return;
            await Shell.Current.GoToAsync($"checklist?id={c.Id}");
        });

        DeleteChecklistCommand = new Command<Checklist>(async c =>
        {
            if (c is null) return;
            await DeleteTemplateAsync(c);
        });

        RestoreActionCommand = new Command<ArchivedAction>(async a =>
        {
            if (a is null) return;
            await ChecklistStore.RestoreAsync(a);
            RefreshView();
        });

        DeleteArchiveCommand = new Command<ArchivedAction>(async a =>
        {
            if (a is null) return;
            await ChecklistStore.DeleteArchivedAsync(a);
            RefreshView();
        });

        ClearArchiveCommand = new Command(async () => await ClearArchiveAsync());
    }

    public async Task InitAsync()
    {
        if (_initialized) return;
        await ChecklistStore.InitAsync(Path.Combine(FileSystem.AppDataDirectory, "checklist.db3"));
        _initialized = true;
        RefreshView();
    }

    public void RefreshView()
    {
        if (IsTemplatesView)
        {
            Templates.Clear();
            foreach (var c in ChecklistStore.Checklists)
                Templates.Add(c);
        }
        else
        {
            Archive.Clear();
            foreach (var a in ChecklistStore.Archive)
                Archive.Add(a);
        }
    }

    private async Task DeleteTemplateAsync(Checklist c)
    {
        bool confirm = await Shell.Current.DisplayAlert(
            "Удалить шаблон?",
            $"«{c.Name}» будет удалён. Записи в архиве сохранятся.",
            "Удалить", "Отмена");
        if (!confirm) return;

        await ChecklistStore.DeleteChecklistAsync(c);
        RefreshView();
    }

    private async Task ClearArchiveAsync()
    {
        if (ChecklistStore.Archive.Count == 0) return;

        bool confirm = await Shell.Current.DisplayAlert(
            "Очистить архив?",
            "Все записи архива будут удалены без возможности восстановления.",
            "Очистить", "Отмена");
        if (!confirm) return;

        await ChecklistStore.ClearArchiveAsync();
        RefreshView();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}