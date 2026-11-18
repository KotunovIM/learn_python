using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
namespace AndroidApp.Models;

public class Checklist : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _accentColorHex = "#512BD4";

    public string Id { get; set; } = Guid.NewGuid().ToString();

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

    public string AccentColorHex
    {
        get => _accentColorHex;
        set
        {
            if (_accentColorHex == value) return;
            _accentColorHex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AccentColor));
        }
    }

    [JsonIgnore]
    public Color AccentColor => Color.FromArgb(AccentColorHex);

    public ObservableCollection<ChecklistItem> Items { get; set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}