using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using AndroidApp.Models;
using AndroidApp.Services;

namespace AndroidApp.PageModels;

public class ChecklistDetailPageModel : INotifyPropertyChanged
{
    private Checklist? _checklist;
    private string _newItemText = string.Empty;

    public Checklist? Checklist
    {
        get => _checklist;
        private set
        {
            if (_checklist == value) return;
            _checklist = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChecklist));
        }
    }

    public bool HasChecklist => Checklist is not null;

    public string NewItemText
    {
        get => _newItemText;
        set { if (_newItemText == value) return; _newItemText = value; OnPropertyChanged(); }
    }

    public ICommand AddItemCommand    { get; }
    public ICommand RemoveItemCommand { get; }

    public ChecklistDetailPageModel()
    {
        AddItemCommand    = new Command(async () => await AddItemAsync());
        RemoveItemCommand = new Command<ChecklistItem>(async i => await RemoveItemAsync(i));
    }

    public void LoadChecklist(string id)
    {
        Checklist = ChecklistStore.Checklists.FirstOrDefault(c => c.Id == id);
    }

    /// <summary>
    /// Тап по пункту: пишет в архив, но НЕ удаляет из шаблона.
    /// Галочка (IsDone) сбрасывается только при закрытии страницы.
    /// </summary>
    public async Task ArchiveItemAsync(ChecklistItem? item)
    {
        if (Checklist is null || item is null) return;
        await ChecklistStore.ArchiveAsync(Checklist, item);
    }

    /// <summary>Удаляет действие из шаблона (не из архива).</summary>
    public async Task RemoveItemAsync(ChecklistItem? item)
    {
        if (Checklist is null || item is null) return;
        Checklist.Items.Remove(item);
        await ChecklistStore.SaveChecklistAsync(Checklist);
    }

    private async Task AddItemAsync()
    {
        if (Checklist is null || string.IsNullOrWhiteSpace(NewItemText))
            return;

        Checklist.Items.Add(new ChecklistItem { Text = NewItemText.Trim() });
        NewItemText = string.Empty;
        await ChecklistStore.SaveChecklistAsync(Checklist);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}