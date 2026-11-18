using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using AndroidApp.Models;
using AndroidApp.Services;

namespace AndroidApp.PageModels;

public class ArchivePageModel : INotifyPropertyChanged
{
    private bool _initialized;

    public ObservableCollection<ArchivedAction> Archive { get; } = new();

    public ICommand RestoreActionCommand { get; }
    public ICommand DeleteArchiveCommand { get; }
    public ICommand ClearArchiveCommand  { get; }

    public ArchivePageModel()
    {
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
    }

    public void RefreshView()
    {
        Archive.Clear();
        foreach (var a in ChecklistStore.Archive)
            Archive.Add(a);
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