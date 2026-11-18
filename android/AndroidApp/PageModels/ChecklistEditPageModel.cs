using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using AndroidApp.Models;
using AndroidApp.Services;

namespace AndroidApp.PageModels;

public class ChecklistEditPageModel : INotifyPropertyChanged
{
    private Checklist? _checklist;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _selectedColorName = "Фиолетовый";

    public string? ChecklistId { get; private set; }
    public bool IsNew     => string.IsNullOrEmpty(ChecklistId);
    public string PageTitle => IsNew ? "Новый чек-лист" : "Редактировать";

    public string Name
    {
        get => _name;
        set { if (_name == value) return; _name = value; OnPropertyChanged(); }
    }

    public string Description
    {
        get => _description;
        set { if (_description == value) return; _description = value; OnPropertyChanged(); }
    }

    public ObservableCollection<string> ColorOptions { get; } = new()
    {
        "Фиолетовый", "Маджента", "Индиго", "Лаванда"
    };

    public string SelectedColorName
    {
        get => _selectedColorName;
        set
        {
            if (_selectedColorName == value) return;
            _selectedColorName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AccentColorHex));
        }
    }

    public string AccentColorHex => SelectedColorName switch
    {
        "Маджента" => "#D600AA",
        "Индиго"   => "#2B0B98",
        "Лаванда"  => "#ac99ea",
        _          => "#512BD4"
    };

    public ICommand SaveCommand   { get; }
    public ICommand CancelCommand { get; }

    public ChecklistEditPageModel()
    {
        SaveCommand   = new Command(async () => await SaveAsync());
        CancelCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    public void LoadOrCreate(string? id)
    {
        ChecklistId = id;
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(IsNew));

        if (string.IsNullOrEmpty(id))
        {
            Name = "";
            Description = "";
            SelectedColorName = "Фиолетовый";
            return;
        }

        _checklist = ChecklistStore.Checklists.FirstOrDefault(c => c.Id == id);
        if (_checklist is null) return;

        Name = _checklist.Name;
        Description = _checklist.Description;
        SelectedColorName = _checklist.AccentColorHex switch
        {
            "#D600AA" => "Маджента",
            "#2B0B98" => "Индиго",
            "#ac99ea" => "Лаванда",
            _         => "Фиолетовый"
        };
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            await Shell.Current.DisplayAlert("Ошибка", "Введите название", "OK");
            return;
        }

        if (IsNew)
        {
            var c = new Checklist
            {
                Name = Name.Trim(),
                Description = Description?.Trim() ?? "",
                AccentColorHex = AccentColorHex
            };
            await ChecklistStore.AddChecklistAsync(c);
        }
        else if (_checklist is not null)
        {
            _checklist.Name = Name.Trim();
            _checklist.Description = Description?.Trim() ?? "";
            _checklist.AccentColorHex = AccentColorHex;
            await ChecklistStore.SaveChecklistAsync(_checklist);
        }

        await Shell.Current.GoToAsync("..");
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}