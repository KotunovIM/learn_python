using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CheckListApp;

public class ActionLogEntry
{
    public string Operation { get; set; } = "";
    public string Action { get; set; } = "";
    public string ConfirmedAt { get; set; } = "";
}

public class OperationLogEntry
{
    public string Operation { get; set; } = "";
    public string StartedAt { get; set; } = "";
    public string FinishedAt { get; set; } = "";
}

public class OperationVm : INotifyPropertyChanged
{
    private string _name = "";
    public long Id { get; set; }
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }
    public ObservableCollection<string> Actions { get; } = new();
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}