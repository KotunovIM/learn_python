# ============================================================
#  CheckListApp — полная пересборка под .NET 10
# ============================================================
$ErrorActionPreference = "Stop"

$Root = "C:\GIT\github"
$Name = "CheckListApp"
$Path = Join-Path $Root $Name
$Old  = Join-Path $Root "${Name}_old"

if (Test-Path $Path) {
    if (Test-Path $Old) { Remove-Item $Old -Recurse -Force }
    Rename-Item $Path $Old
    Write-Host "Старая папка -> $Old" -ForegroundColor Yellow
}

Set-Location $Root
dotnet new maui -n $Name
Set-Location $Path

Remove-Item MainPage.xaml, MainPage.xaml.cs -ErrorAction SilentlyContinue
Remove-Item AppShell.xaml, AppShell.xaml.cs -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path "Views" -Force | Out-Null

function Write-Utf8($file, $content) {
    [System.IO.File]::WriteAllText((Join-Path $Path $file), $content, (New-Object System.Text.UTF8Encoding $false))
}

# ---------- csproj (net10 + SQLite) ----------
$csproj = Get-Content "$Name.csproj" -Raw
$csproj = $csproj -replace 'net8\.0', 'net10.0' -replace 'net9\.0', 'net10.0'
if ($csproj -notmatch 'Microsoft\.Data\.Sqlite') {
    $csproj = $csproj -replace '</Project>', @'
  <ItemGroup>
    <PackageReference Include="Microsoft.Data.Sqlite" Version="9.0.0" />
  </ItemGroup>
</Project>
'@
}
Write-Utf8 "$Name.csproj" $csproj

# ---------- MauiProgram.cs ----------
Write-Utf8 "MauiProgram.cs" @'
using Microsoft.Extensions.Logging;

namespace CheckListApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
'@

# ---------- App.xaml ----------
Write-Utf8 "App.xaml" @'
<?xml version="1.0" encoding="UTF-8" ?>
<Application xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="CheckListApp.App">
    <Application.Resources>
        <ResourceDictionary>
            <Color x:Key="Primary">#1E90FF</Color>
            <Color x:Key="Danger">#E84118</Color>
            <Color x:Key="Bg">#F5F6FA</Color>
            <Color x:Key="CardBg">#FFFFFF</Color>
            <Color x:Key="TextMain">#2F3640</Color>
            <Color x:Key="TextMuted">#718093</Color>

            <Style TargetType="Button">
                <Setter Property="BackgroundColor" Value="{StaticResource CardBg}"/>
                <Setter Property="TextColor" Value="{StaticResource TextMain}"/>
                <Setter Property="BorderColor" Value="#DCDDE1"/>
                <Setter Property="BorderWidth" Value="1"/>
                <Setter Property="CornerRadius" Value="6"/>
                <Setter Property="Padding" Value="10,6"/>
                <Setter Property="FontSize" Value="14"/>
            </Style>
            <Style x:Key="PrimaryBtn" TargetType="Button" BasedOn="{StaticResource {x:Type Button}}">
                <Setter Property="BackgroundColor" Value="{StaticResource Primary}"/>
                <Setter Property="TextColor" Value="White"/>
                <Setter Property="FontAttributes" Value="Bold"/>
            </Style>
            <Style x:Key="DangerBtn" TargetType="Button" BasedOn="{StaticResource {x:Type Button}}">
                <Setter Property="BackgroundColor" Value="{StaticResource Danger}"/>
                <Setter Property="TextColor" Value="White"/>
                <Setter Property="FontAttributes" Value="Bold"/>
            </Style>
            <Style x:Key="SectionHeader" TargetType="Label">
                <Setter Property="FontSize" Value="15"/>
                <Setter Property="FontAttributes" Value="Bold"/>
                <Setter Property="TextColor" Value="{StaticResource TextMain}"/>
                <Setter Property="Margin" Value="0,8,0,4"/>
            </Style>
        </ResourceDictionary>
    </Application.Resources>
</Application>
'@

# ---------- App.xaml.cs ----------
Write-Utf8 "App.xaml.cs" @'
namespace CheckListApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        Storage.InitDb();
        MainPage = new NavigationPage(new Views.OperationsPage());
    }
}
'@

# ---------- Models.cs ----------
Write-Utf8 "Models.cs" @'
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
'@

# ---------- Converters.cs ----------
Write-Utf8 "Converters.cs" @'
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
'@

# ---------- Storage.cs ----------
Write-Utf8 "Storage.cs" @'
using Microsoft.Data.Sqlite;

namespace CheckListApp;

public static class Storage
{
    private static readonly string BaseDir = FileSystem.AppDataDirectory;
    public static string DbFile => Path.Combine(BaseDir, "journal.db");

    private static SqliteConnection Open()
    {
        var conn = new SqliteConnection($"Data Source={DbFile}");
        conn.Open();
        using var pragma = conn.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();
        return conn;
    }

    public static void InitDb()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS operations (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                created_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS actions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                operation_id INTEGER NOT NULL,
                text TEXT NOT NULL,
                position INTEGER NOT NULL,
                FOREIGN KEY (operation_id) REFERENCES operations(id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS idx_actions_op ON actions(operation_id, position);
            CREATE TABLE IF NOT EXISTS action_log (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                operation TEXT NOT NULL,
                action TEXT NOT NULL,
                confirmed_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS operation_log (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                operation TEXT NOT NULL,
                started_at TEXT NOT NULL,
                finished_at TEXT NOT NULL
            );
        ";
        cmd.ExecuteNonQuery();
        SeedDefaultsIfEmpty(conn);
    }

    private static void SeedDefaultsIfEmpty(SqliteConnection conn)
    {
        using (var check = conn.CreateCommand())
        {
            check.CommandText = "SELECT COUNT(*) FROM operations;";
            if (Convert.ToInt64(check.ExecuteScalar()) > 0) return;
        }
        AddOperationWithActions(conn, "Выхожу из дома",
            new List<string> { "Выключил газ", "Выключил свет", "Перекрыл воду", "Закрыл окна", "Закрыл дверь" });
        AddOperationWithActions(conn, "Ложусь спать",
            new List<string> { "Выключил технику", "Поставил будильник", "Погасил свет" });
    }

    private static void AddOperationWithActions(SqliteConnection conn, string name, List<string> actions)
    {
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        long opId;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO operations (name, created_at) VALUES ($n, $t); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$n", name);
            cmd.Parameters.AddWithValue("$t", now);
            opId = Convert.ToInt64(cmd.ExecuteScalar());
        }
        for (int i = 0; i < actions.Count; i++)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO actions (operation_id, text, position) VALUES ($op, $txt, $pos);";
            cmd.Parameters.AddWithValue("$op", opId);
            cmd.Parameters.AddWithValue("$txt", actions[i]);
            cmd.Parameters.AddWithValue("$pos", i);
            cmd.ExecuteNonQuery();
        }
    }

    public static List<OperationVm> LoadOperations()
    {
        var list = new List<OperationVm>();
        using var conn = Open();
        var byId = new Dictionary<long, OperationVm>();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT id, name FROM operations ORDER BY id;";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var vm = new OperationVm { Id = r.GetInt64(0), Name = r.GetString(1) };
                list.Add(vm);
                byId[vm.Id] = vm;
            }
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT operation_id, text FROM actions ORDER BY operation_id, position;";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var opId = r.GetInt64(0);
                if (byId.TryGetValue(opId, out var vm)) vm.Actions.Add(r.GetString(1));
            }
        }
        return list;
    }

    public static bool OperationNameExists(string name)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM operations WHERE name = $n;";
        cmd.Parameters.AddWithValue("$n", name);
        return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
    }

    public static long CreateOperation(string name)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO operations (name, created_at) VALUES ($n, $t); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$n", name);
        cmd.Parameters.AddWithValue("$t", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    public static void DeleteOperation(long opId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM operations WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", opId);
        cmd.ExecuteNonQuery();
    }

    public static void AddAction(long opId, string text)
    {
        using var conn = Open();
        long nextPos;
        using (var q = conn.CreateCommand())
        {
            q.CommandText = "SELECT COALESCE(MAX(position), -1) + 1 FROM actions WHERE operation_id = $op;";
            q.Parameters.AddWithValue("$op", opId);
            nextPos = Convert.ToInt64(q.ExecuteScalar());
        }
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO actions (operation_id, text, position) VALUES ($op, $txt, $pos);";
        cmd.Parameters.AddWithValue("$op", opId);
        cmd.Parameters.AddWithValue("$txt", text);
        cmd.Parameters.AddWithValue("$pos", nextPos);
        cmd.ExecuteNonQuery();
    }

    public static void DeleteLastAction(long opId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM actions WHERE id = (SELECT id FROM actions WHERE operation_id = $op ORDER BY position DESC LIMIT 1);";
        cmd.Parameters.AddWithValue("$op", opId);
        cmd.ExecuteNonQuery();
    }

    public static string LogAction(string operation, string action)
    {
        var ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO action_log (operation, action, confirmed_at) VALUES ($op, $act, $ts);";
        cmd.Parameters.AddWithValue("$op", operation);
        cmd.Parameters.AddWithValue("$act", action);
        cmd.Parameters.AddWithValue("$ts", ts);
        cmd.ExecuteNonQuery();
        return ts;
    }

    public static string LogOperation(string operation, string startedAt)
    {
        var fin = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO operation_log (operation, started_at, finished_at) VALUES ($op, $st, $fin);";
        cmd.Parameters.AddWithValue("$op", operation);
        cmd.Parameters.AddWithValue("$st", startedAt);
        cmd.Parameters.AddWithValue("$fin", fin);
        cmd.ExecuteNonQuery();
        return fin;
    }

    public static List<ActionLogEntry> GetRecentActions(int limit = 100)
    {
        var list = new List<ActionLogEntry>();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT operation, action, confirmed_at FROM action_log ORDER BY id DESC LIMIT $lim;";
        cmd.Parameters.AddWithValue("$lim", limit);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new ActionLogEntry { Operation = r.GetString(0), Action = r.GetString(1), ConfirmedAt = r.GetString(2) });
        return list;
    }

    public static List<OperationLogEntry> GetRecentOperations(int limit = 50)
    {
        var list = new List<OperationLogEntry>();
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT operation, started_at, finished_at FROM operation_log ORDER BY id DESC LIMIT $lim;";
        cmd.Parameters.AddWithValue("$lim", limit);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new OperationLogEntry { Operation = r.GetString(0), StartedAt = r.GetString(1), FinishedAt = r.GetString(2) });
        return list;
    }
}
'@

Write-Host "Базовые файлы готовы. Пишу Views/..." -ForegroundColor Cyan

# ---------- Views/OperationsPage.xaml ----------
Write-Utf8 "Views\OperationsPage.xaml" @'
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="CheckListApp.Views.OperationsPage"
             Title="Операции"
             BackgroundColor="{StaticResource Bg}">
    <Grid RowDefinitions="Auto,*,Auto,Auto,Auto,Auto" Padding="12" RowSpacing="8">
        <Label Grid.Row="0" Text="Операции" Style="{StaticResource SectionHeader}"/>
        <Border Grid.Row="1" BackgroundColor="White" StrokeShape="RoundRectangle 8" Stroke="#DCDDE1">
            <CollectionView x:Name="CvOperations" SelectionMode="Single"
                            SelectionChanged="CvOperations_SelectionChanged">
                <CollectionView.ItemTemplate>
                    <DataTemplate>
                        <Grid Padding="12,10" ColumnDefinitions="*,Auto">
                            <Label Grid.Column="0" Text="{Binding Name}" FontSize="15" VerticalOptions="Center"/>
                            <Label Grid.Column="1" Text="{Binding Actions.Count}" FontSize="12"
                                   TextColor="{StaticResource TextMuted}" VerticalOptions="Center"/>
                        </Grid>
                    </DataTemplate>
                </CollectionView.ItemTemplate>
            </CollectionView>
        </Border>
        <HorizontalStackLayout Grid.Row="2" Spacing="6">
            <Button Text="+ Операция" Clicked="AddOperation_Click"/>
            <Button Text="− Удалить" Clicked="DelOperation_Click"/>
            <Button Text="▶ Выполнить" Style="{StaticResource PrimaryBtn}" Clicked="RunOperation_Click"/>
        </HorizontalStackLayout>
        <Label Grid.Row="3" x:Name="LblActionsHeader" Text="Действия операции" Style="{StaticResource SectionHeader}"/>
        <Border Grid.Row="4" BackgroundColor="White" StrokeShape="RoundRectangle 8" Stroke="#DCDDE1" HeightRequest="220">
            <CollectionView x:Name="CvActions" SelectionMode="Single">
                <CollectionView.ItemTemplate>
                    <DataTemplate>
                        <Label Padding="12,8" Text="{Binding .}" FontSize="14"/>
                    </DataTemplate>
                </CollectionView.ItemTemplate>
            </CollectionView>
        </Border>
        <HorizontalStackLayout Grid.Row="5" Spacing="6">
            <Button Text="+ Действие" Clicked="AddAction_Click"/>
            <Button Text="− Действие" Clicked="DelAction_Click"/>
        </HorizontalStackLayout>
    </Grid>
</ContentPage>
'@

# ---------- Views/OperationsPage.xaml.cs ----------
Write-Utf8 "Views\OperationsPage.xaml.cs" @'
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
        bool ok = await DisplayAlert("Удаление", $"Удалить операцию «{_selected.Name}» и все её действия?", "Да", "Нет");
        if (!ok) return;
        Storage.DeleteOperation(_selected.Id);
        _selected = null;
        Reload();
    }

    private async void AddAction_Click(object sender, EventArgs e)
    {
        if (_selected == null) { await DisplayAlert("Внимание", "Сначала выберите операцию.", "OK"); return; }
        var text = await DisplayPromptAsync("Новое действие", "Описание:");
        if (string.IsNullOrWhiteSpace(text)) return;
        text = text.Trim();
        Storage.AddAction(_selected.Id, text);
        _selected.Actions.Add(text);
    }

    private async void DelAction_Click(object sender, EventArgs e)
    {
        if (_selected == null || _selected.Actions.Count == 0) return;
        bool ok = await DisplayAlert("Удаление", $"Удалить последнее действие «{_selected.Actions[^1]}»?", "Да", "Нет");
        if (!ok) return;
        Storage.DeleteLastAction(_selected.Id);
        _selected.Actions.RemoveAt(_selected.Actions.Count - 1);
    }

    private async void RunOperation_Click(object sender, EventArgs e)
    {
        if (_selected == null) { await DisplayAlert("Внимание", "Выберите операцию.", "OK"); return; }
        if (_selected.Actions.Count == 0) { await DisplayAlert("Внимание", "У операции нет действий.", "OK"); return; }
        var run = new RunPage();
        run.Start(_selected.Name, _selected.Actions.ToList());
        await Navigation.PushAsync(run);
    }
}
'@

# ---------- Views/RunPage.xaml ----------
Write-Utf8 "Views\RunPage.xaml" @'
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="CheckListApp.Views.RunPage"
             Title="Выполнение"
             BackgroundColor="{StaticResource Bg}">
    <Grid Padding="16" RowDefinitions="Auto,Auto,Auto,*,Auto,Auto" RowSpacing="10">
        <Label x:Name="LblTitle" Grid.Row="0" Text="—" FontSize="18" FontAttributes="Bold"
               HorizontalOptions="Center" TextColor="{StaticResource TextMain}"/>
        <Label x:Name="LblStep" Grid.Row="1" Text="" FontSize="13"
               TextColor="{StaticResource TextMuted}" HorizontalOptions="Center"/>
        <ProgressBar x:Name="Progress" Grid.Row="2" ProgressColor="{StaticResource Primary}" Progress="0"/>
        <Border Grid.Row="3" BackgroundColor="White" Stroke="#DCDDE1" StrokeShape="RoundRectangle 12" Padding="20">
            <Label x:Name="LblAction" Text="—" FontSize="26" TextColor="{StaticResource TextMain}"
                   HorizontalOptions="Center" VerticalOptions="Center" HorizontalTextAlignment="Center"/>
        </Border>
        <Label x:Name="LblStatus" Grid.Row="4" Text="" TextColor="#009432"
               FontAttributes="Bold" HorizontalOptions="Center"/>
        <Grid Grid.Row="5" ColumnDefinitions="*,*" ColumnSpacing="8">
            <Button x:Name="BtnConfirm" Grid.Column="0" Text="✔ Подтвердить"
                    Style="{StaticResource PrimaryBtn}" IsEnabled="False" Clicked="Confirm_Click"/>
            <Button x:Name="BtnAbort" Grid.Column="1" Text="✖ Прервать"
                    Style="{StaticResource DangerBtn}" IsEnabled="False" Clicked="Abort_Click"/>
        </Grid>
    </Grid>
</ContentPage>
'@

# ---------- Views/RunPage.xaml.cs ----------
Write-Utf8 "Views\RunPage.xaml.cs" @'
namespace CheckListApp.Views;

public partial class RunPage : ContentPage
{
    private string _opName = "";
    private List<string> _actions = new();
    private int _index;
    private string _startedAt = "";

    public RunPage() { InitializeComponent(); }

    public void Start(string opName, List<string> actions)
    {
        _opName = opName;
        _actions = actions;
        _index = 0;
        _startedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        LblTitle.Text = $"Операция: {_opName}";
        Progress.Progress = 0;
        LblStatus.Text = $"Начато: {_startedAt}";
        BtnConfirm.IsEnabled = true;
        BtnAbort.IsEnabled = true;
        ShowStep();
    }

    private void ShowStep()
    {
        if (_index >= _actions.Count) { FinishOperation(); return; }
        LblStep.Text = $"Шаг {_index + 1} из {_actions.Count}";
        LblAction.Text = _actions[_index];
        Progress.Progress = (double)_index / _actions.Count;
    }

    private void Confirm_Click(object sender, EventArgs e)
    {
        if (_index >= _actions.Count) return;
        var action = _actions[_index];
        var ts = Storage.LogAction(_opName, action);
        LblStatus.Text = $"✔ «{action}» — {ts}";
        _index++;
        Progress.Progress = (double)_index / _actions.Count;
        ShowStep();
    }

    private void FinishOperation()
    {
        var fin = Storage.LogOperation(_opName, _startedAt);
        LblAction.Text = $"✅ Операция «{_opName}» завершена";
        LblStep.Text = "";
        Progress.Progress = 1;
        LblStatus.Text = $"Завершено: {fin}";
        BtnConfirm.IsEnabled = false;
        BtnAbort.IsEnabled = false;
    }

    private async void Abort_Click(object sender, EventArgs e)
    {
        bool ok = await DisplayAlert("Прервать", "Прервать выполнение?", "Да", "Нет");
        if (!ok) return;
        LblAction.Text = $"Операция «{_opName}» прервана";
        LblStep.Text = "";
        LblStatus.Text = "Прервано пользователем.";
        BtnConfirm.IsEnabled = false;
        BtnAbort.IsEnabled = false;
    }
}
'@

# ---------- Views/LogPage.xaml ----------
Write-Utf8 "Views\LogPage.xaml" @'
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:local="clr-namespace:CheckListApp"
             x:Class="CheckListApp.Views.LogPage"
             Title="Журнал"
             BackgroundColor="{StaticResource Bg}">
    <ContentPage.Resources>
        <local:ActionLogToStringConverter x:Key="ActionConv"/>
        <local:OpLogToStringConverter x:Key="OpConv"/>
    </ContentPage.Resources>
    <Grid Padding="12" RowDefinitions="Auto,Auto,2*,Auto,1*" RowSpacing="8">
        <Button Grid.Row="0" Text="🔄 Обновить" HorizontalOptions="Start" Clicked="Refresh_Click"/>
        <Label Grid.Row="1" Text="Подтверждения действий (последние 100)" Style="{StaticResource SectionHeader}"/>
        <Border Grid.Row="2" BackgroundColor="White" Stroke="#DCDDE1" StrokeShape="RoundRectangle 8">
            <CollectionView x:Name="CvActions">
                <CollectionView.ItemTemplate>
                    <DataTemplate>
                        <Label Padding="10,6" FontSize="12"
                               Text="{Binding ., Converter={StaticResource ActionConv}}"/>
                    </DataTemplate>
                </CollectionView.ItemTemplate>
            </CollectionView>
        </Border>
        <Label Grid.Row="3" Text="Завершённые операции (последние 50)" Style="{StaticResource SectionHeader}"/>
        <Border Grid.Row="4" BackgroundColor="White" Stroke="#DCDDE1" StrokeShape="RoundRectangle 8">
            <CollectionView x:Name="CvOps">
                <CollectionView.ItemTemplate>
                    <DataTemplate>
                        <Label Padding="10,6" FontSize="12"
                               Text="{Binding ., Converter={StaticResource OpConv}}"/>
                    </DataTemplate>
                </CollectionView.ItemTemplate>
            </CollectionView>
        </Border>
    </Grid>
</ContentPage>
'@

# ---------- Views/LogPage.xaml.cs ----------
Write-Utf8 "Views\LogPage.xaml.cs" @'
namespace CheckListApp.Views;

public partial class LogPage : ContentPage
{
    public LogPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
    }

    private void Refresh_Click(object sender, EventArgs e) => Refresh();

    private void Refresh()
    {
        CvActions.ItemsSource = Storage.GetRecentActions(100);
        CvOps.ItemsSource = Storage.GetRecentOperations(50);
    }
}
'@

# ---------- Добавить кнопку "Журнал" на главную ----------
$opsXamlPath = Join-Path $Path "Views\OperationsPage.xaml"
$xaml = Get-Content $opsXamlPath -Raw
if ($xaml -notmatch 'BtnLog') {
    $xaml = $xaml -replace '(</Grid>\s*</ContentPage>)', @'
        <Button x:Name="BtnLog" Text="📋 Журнал" Clicked="OpenLog_Click"
                Grid.Row="5" HorizontalOptions="End" WidthRequest="110"/>
    </Grid>
</ContentPage>
'@
    [System.IO.File]::WriteAllText($opsXamlPath, $xaml, (New-Object System.Text.UTF8Encoding $false))
}

$opsCsPath = Join-Path $Path "Views\OperationsPage.xaml.cs"
$cs = Get-Content $opsCsPath -Raw
if ($cs -notmatch 'OpenLog_Click') {
    $cs = $cs -replace '(private async void RunOperation_Click)', @'
    private async void OpenLog_Click(object sender, EventArgs e)
        => await Navigation.PushAsync(new LogPage());

    private async void RunOperation_Click
'@
    [System.IO.File]::WriteAllText($opsCsPath, $cs, (New-Object System.Text.UTF8Encoding $false))
}

# ---------- Сборка ----------
Write-Host ""
Write-Host "Все файлы записаны. Запускаю сборку..." -ForegroundColor Cyan
dotnet restore
dotnet build -f net10.0-android -c Debug

Write-Host ""
Write-Host "Готово. Если сборка прошла — APK лежит в:" -ForegroundColor Green
Write-Host "  $Path\bin\Debug\net10.0-android\*.apk" -ForegroundColor Green