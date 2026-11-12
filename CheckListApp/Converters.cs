using System.Globalization;

namespace CheckListApp;

public class ActionLogToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is ActionLogEntry e ? $"[{e.ConfirmedAt}]  {e.Operation}\n    -> {e.Action}" : "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class OpLogToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is OperationLogEntry e ? $"[{e.FinishedAt}]  {e.Operation}\n    start: {e.StartedAt}" : "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}