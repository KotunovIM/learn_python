using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace AndroidApp.Models;

public class ChecklistItem : INotifyPropertyChanged
{
    private bool _isDone;

    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Только для UI: галочка + зачёркивание.
    /// Не сохраняется в JSON шаблона и сбрасывается при закрытии страницы.
    /// </summary>
    [JsonIgnore]
    public bool IsDone
    {
        get => _isDone;
        set
        {
            if (_isDone == value) return;
            _isDone = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}