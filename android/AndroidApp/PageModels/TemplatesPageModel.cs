using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using AndroidApp.Models;
using AndroidApp.Services;

namespace AndroidApp.PageModels;

public class TemplatesPageModel : INotifyPropertyChanged
{
    private bool _initialized;

    public ObservableCollection<Checklist>      Templates     { get; } = new();
    public ObservableCollection<ArchivedAction> RecentArchive { get; } = new();

    public ICommand AddChecklistCommand    { get; }
    public ICommand EditChecklistCommand   { get; }
    public ICommand DeleteChecklistCommand { get; }
    public ICommand OpenArchiveCommand     { get; }

    public TemplatesPageModel()
    {
        AddChecklistCommand = new Command(async () =>
            await Shell.Current.GoToAsync("checklist-edit"));

        EditChecklistCommand = new Command<Checklist>(async c =>
        {
            if (c is null) return;
            await Shell.Current.GoToAsync($"checklist-edit?id={c.Id}");
        });

        DeleteChecklistCommand = new Command<Checklist>(async c =>
        {
            if (c is null) return;
            await DeleteTemplateAsync(c);
        });

        OpenArchiveCommand = new Command(async () =>
            await Shell.Current.GoToAsync("//archive"));
    }

    public async Task InitAsync()
    {
        if (_initialized) return;
        await ChecklistStore.InitAsync(Path.Combine(FileSystem.AppDataDirectory, "checklist.db3"));
        _initialized = true;
    }

    public void RefreshView()
    {
        Templates.Clear();
        foreach (var c in ChecklistStore.Checklists)
            Templates.Add(c);

        RecentArchive.Clear();
        foreach (var a in ChecklistStore.Archive.Take(20))
            RecentArchive.Add(a);
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

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}