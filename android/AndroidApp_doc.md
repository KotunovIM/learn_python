# CheckListApp

Менеджер повторяющихся действий на .NET MAUI для Android.

- **Шаблоны** — списки действий, которые нужно выполнять повторно (Покупки, Учёба, Дом).
- **Архив** — история выполненных действий с датой и именем шаблона.
- **Повторяемость** — действие остаётся в шаблоне после выполнения, чтобы его можно было выполнить снова.
- **Хранение** — SQLite (`sqlite-net-pcl`).

---

## 📱 Функциональность

| Функция | Описание |
|---|---|
| Шаблоны | Пользователь создаёт шаблоны; каждый — список действий |
| Выполнение | Тап → ✓ + зачёркивание → запись в архив. **Строка остаётся** в шаблоне |
| Архив | Все выполнения с датой и именем шаблона |
| Возврат | Свайп по архиву → «Вернуть» / «Удалить» |
| Создание/редактирование | `ChecklistEditPage`: название, описание, цвет |
| Удаление шаблона | Свайп вверх по карточке → «Удалить» (архив сохраняется) |
| Фильтр | «Шаблонные» / «Архив действий» в верхнем левом углу |
| Сохранение | SQLite `checklist.db3` в `FileSystem.AppDataDirectory` |
| Тема | `AppThemeBinding` для светлой/тёмной |

**Поведение галочки:**
- Тап → ✓ + зачёркивание → запись в архив.
- Галочка **не сбрасывается** пока страница открыта.
- При закрытии (`OnDisappearing`) `IsDone = false` у всех действий.
- `IsDone` помечен `[JsonIgnore]` — в БД не сохраняется.

---

## 🏗️ Архитектура

```
┌──────────────────────────────────────────┐
│  Pages/ — View (XAML + code-behind)      │
│  MainPage, ChecklistDetailPage,          │
│  ChecklistEditPage                       │
└──────────────┬───────────────────────────┘
               │ Binding
┌──────────────▼───────────────────────────┐
│  PageModels/ — ViewModel                 │
│  MainPageModel, ChecklistDetailPageModel,│
│  ChecklistEditPageModel                  │
└──────────────┬───────────────────────────┘
               │
┌──────────────▼───────────────────────────┐
│  Services/ — доступ к данным             │
│  ChecklistStore (static cache),          │
│  ChecklistDatabase (SQLite)              │
└──────────────┬───────────────────────────┘
               │
┌──────────────▼───────────────────────────┐
│  Models/ — сущности                      │
│  Checklist, ChecklistItem, ArchivedAction│
└──────────────────────────────────────────┘

┌──────────────────────────────────────────┐
│  Controls/ — переиспользуемые View       │
│  ChecklistCardView                       │
└──────────────────────────────────────────┘
```

**Принципы:**
1. `ChecklistStore` — статический кэш `ObservableCollection`, синхронизируется с БД.
2. `ChecklistDatabase` — единственное место работы с SQLite. Две таблицы: `checklists`, `archived_actions`. Модели хранятся в JSON-колонке.
3. `PageModels` не работают с SQLite напрямую — только через `ChecklistStore`.
4. `IsDone` — UI-only (в БД не попадает), `ArchivedAction` — реальная запись в БД.

---

## 📁 Структура проекта

```
CheckListApp/
├── App.xaml(.cs)                     # стандарт MAUI
├── AppShell.xaml(.cs)                # + маршруты
├── MauiProgram.cs                    # стандарт MAUI
├── CheckListApp.csproj
│
├── Models/
│   ├── Checklist.cs
│   ├── ChecklistItem.cs
│   └── ArchivedAction.cs
│
├── Services/
│   ├── ChecklistDatabase.cs
│   └── ChecklistStore.cs
│
├── PageModels/
│   ├── MainPageModel.cs
│   ├── ChecklistDetailPageModel.cs
│   └── ChecklistEditPageModel.cs
│
├── Controls/
│   ├── ChecklistCardView.xaml
│   └── ChecklistCardView.xaml.cs
│
├── Pages/
│   ├── MainPage.xaml(.cs)
│   ├── ChecklistDetailPage.xaml(.cs)
│   └── ChecklistEditPage.xaml(.cs)
│
├── Platforms/Android/
│   ├── MainActivity.cs               # стандарт MAUI
│   ├── MainApplication.cs
│   └── AndroidManifest.xml
│
└── Resources/
    ├── AppIcon/                       # стандарт MAUI
    ├── Splash/
    ├── Fonts/
    ├── Images/
    └── Styles/
        ├── Colors.xaml               # + 6 кастомных цветов
        └── Styles.xaml
```

---

## 🚀 Создание проекта с нуля

### 1. Создайте проект по шаблону

```
Visual Studio → Файл → Создать → Проект → .NET MAUI App
Имя: CheckListApp
Framework: .NET 10.0
```

**Никогда** не создавайте MAUI-проект вручную как пустой Android-проект — это источник 90% проблем.

### 2. Проверьте «Hello, World!»

**F5 без единой правки**. Если пустой шаблон не запускается — проблема вне проекта (Android SDK, эмулятор, JDK). Решать до добавления кода.

### 3. Установите NuGet-пакеты

Через **Средства → Диспетчер пакетов NuGet → Консоль диспетчера пакетов**:

```powershell
Install-Package sqlite-net-pcl -ProjectName CheckListApp
Install-Package Syncfusion.Maui.Toolkit -Version 1.0.8 -ProjectName CheckListApp
```

**Правила:**
- `Syncfusion.Maui.Toolkit` — **строго ≥ 1.0.8** для .NET 10.
- `sqlite-net-pcl` — последняя стабильная.
- `Microsoft.Extensions.Logging.Debug` и `Microsoft.Maui.Controls` — **уже в шаблоне**, не устанавливайте.

### 4. Создайте папки и файлы в правильном порядке

После **каждой** папки — `Ctrl+Shift+B`. 0 ошибок → следующая.

| # | Папка | Файлы | Зависит от |
|---|---|---|---|
| 1 | `Models/` | `ChecklistItem`, `Checklist`, `ArchivedAction` | — |
| 2 | `Services/` | `ChecklistDatabase`, `ChecklistStore` | Models |
| 3 | `PageModels/` | 3 модели | Models, Services |
| 4 | `Controls/` | `ChecklistCardView` (XAML + .cs) | Models |
| 5 | `Pages/` | 3 страницы | всё выше |
| 6 | `AppShell.xaml.cs` | +2 строки `Routing.RegisterRoute` | Pages |
| 7 | `AppShell.xaml` | Пути `pages:*` | Pages |
| 8 | F5 | Запуск | всё |

### 5. Обновите `Resources/Styles/Colors.xaml`

Добавьте в конец (перед `</ResourceDictionary>`):

```xml
<Color x:Key="DarkBackground">#17171a</Color>
<Color x:Key="LightOnDarkBackground">#C3C3C3</Color>
<Color x:Key="LightBackground">#F2F2F2</Color>
<Color x:Key="DarkOnLightBackground">#0D0D0D</Color>
<Color x:Key="LightSecondaryBackground">#E0E0E0</Color>
<Color x:Key="DarkSecondaryBackground">#222228</Color>
```

Стандартные ключи (`Primary`, `Gray100..950`, `Magenta`) шаблон уже содержит.

---

## 📦 NuGet-пакеты

| Пакет | Версия | Назначение |
|---|---|---|
| `Microsoft.Maui.Controls` | 10.0.120+ | в шаблоне |
| `Microsoft.Extensions.Logging.Debug` | 10.0.0+ | в шаблоне |
| `sqlite-net-pcl` | 1.11.285+ | SQLite |
| `SQLitePCLRaw.bundle_green` | 2.1.11+ | транзитивно |
| `Syncfusion.Maui.Toolkit` | **≥ 1.0.8** | `SfSegmentedControl` (опционально) |

---

## 💻 Исходный код

### `CheckListApp.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0-android</TargetFramework>
    <SupportedOSPlatformVersion>24</SupportedOSPlatformVersion>
    <OutputType>Exe</OutputType>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <ApplicationId>com.companyname.CheckListApp</ApplicationId>
    <ApplicationVersion>1</ApplicationVersion>
    <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>

    <UseMaui>true</UseMaui>
    <SingleProject>true</SingleProject>
    <TrimMode>full</TrimMode>
  </PropertyGroup>

  <ItemGroup>
    <MauiIcon Include="Resources\AppIcon\appicon.svg"
              ForegroundFile="Resources\AppIcon\appiconfg.svg"
              Color="#512BD4" />
    <MauiSplashScreen Include="Resources\Splash\splash.svg"
                      Color="#512BD4"
                      BaseSize="128,128" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Maui.Controls" Version="$(MauiVersion)" />
    <PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="$(MauiVersion)" />
    <PackageReference Include="sqlite-net-pcl" Version="1.11.285" />
    <PackageReference Include="Syncfusion.Maui.Toolkit" Version="1.0.8" />
  </ItemGroup>

</Project>
```

---

### `Models/ChecklistItem.cs`

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace CheckListApp.Models;

public class ChecklistItem : INotifyPropertyChanged
{
    private bool _isDone;

    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Только для UI: галочка + зачёркивание.
    /// Не сохраняется в JSON и сбрасывается при закрытии страницы.
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
```

---

### `Models/Checklist.cs`

```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace CheckListApp.Models;

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
```

---

### `Models/ArchivedAction.cs`

```csharp
using System.Text.Json.Serialization;

namespace CheckListApp.Models;

public class ArchivedAction
{
    public string   Id             { get; set; } = Guid.NewGuid().ToString();
    public string   ChecklistId    { get; set; } = string.Empty;
    public string   ChecklistName  { get; set; } = string.Empty;
    public string   Text           { get; set; } = string.Empty;
    public DateTime CompletedAt    { get; set; } = DateTime.Now;
    public string   AccentColorHex { get; set; } = "#512BD4";

    [JsonIgnore] public Color  AccentColor     => Color.FromArgb(AccentColorHex);
    [JsonIgnore] public string CompletedAtText => CompletedAt.ToString("dd MMM, HH:mm");
}
```

---

### `Services/ChecklistDatabase.cs`

```csharp
using System.Text.Json;
using CheckListApp.Models;
using SQLite;

namespace CheckListApp.Services;

public class ChecklistDatabase
{
    private readonly string _dbPath;
    private SQLiteAsyncConnection? _db;

    public ChecklistDatabase(string dbPath) => _dbPath = dbPath;

    private async Task<SQLiteAsyncConnection> GetDbAsync()
    {
        if (_db is not null) return _db;
        _db = new SQLiteAsyncConnection(_dbPath);
        await _db.CreateTableAsync<ChecklistRow>();
        await _db.CreateTableAsync<ArchivedActionRow>();
        return _db;
    }

    public async Task<bool> IsEmptyAsync()
    {
        var db = await GetDbAsync();
        return await db.Table<ChecklistRow>().CountAsync() == 0;
    }

    public async Task<List<Checklist>> GetAllChecklistsAsync()
    {
        var db = await GetDbAsync();
        var rows = await db.Table<ChecklistRow>().OrderBy(r => r.SortOrder).ToListAsync();

        var result = new List<Checklist>();
        foreach (var row in rows)
        {
            try
            {
                var c = JsonSerializer.Deserialize<Checklist>(row.Json);
                if (c is not null) result.Add(c);
            }
            catch { /* skip corrupted */ }
        }
        return result;
    }

    public async Task SaveChecklistAsync(Checklist checklist)
    {
        var db = await GetDbAsync();
        await db.InsertOrReplaceAsync(new ChecklistRow
        {
            Id        = checklist.Id,
            Json      = JsonSerializer.Serialize(checklist),
            SortOrder = DateTime.UtcNow.Ticks
        });
    }

    public async Task DeleteChecklistAsync(string id)
    {
        var db = await GetDbAsync();
        await db.DeleteAsync<ChecklistRow>(id);
    }

    public async Task<List<ArchivedAction>> GetArchiveAsync()
    {
        var db = await GetDbAsync();
        var rows = await db.Table<ArchivedActionRow>()
                           .OrderByDescending(r => r.CompletedAt)
                           .ToListAsync();

        var result = new List<ArchivedAction>();
        foreach (var row in rows)
        {
            try
            {
                var a = JsonSerializer.Deserialize<ArchivedAction>(row.Json);
                if (a is not null) result.Add(a);
            }
            catch { /* skip */ }
        }
        return result;
    }

    public async Task SaveArchivedAsync(ArchivedAction action)
    {
        var db = await GetDbAsync();
        await db.InsertOrReplaceAsync(new ArchivedActionRow
        {
            Id          = action.Id,
            Json        = JsonSerializer.Serialize(action),
            CompletedAt = action.CompletedAt
        });
    }

    public async Task DeleteArchivedAsync(string id)
    {
        var db = await GetDbAsync();
        await db.DeleteAsync<ArchivedActionRow>(id);
    }

    public async Task ClearArchiveAsync()
    {
        var db = await GetDbAsync();
        await db.DeleteAllAsync<ArchivedActionRow>();
    }
}

[Table("checklists")]
public class ChecklistRow
{
    [PrimaryKey] public string Id        { get; set; } = string.Empty;
    public string             Json      { get; set; } = string.Empty;
    public long               SortOrder { get; set; }
}

[Table("archived_actions")]
public class ArchivedActionRow
{
    [PrimaryKey] public string   Id          { get; set; } = string.Empty;
    public string                Json        { get; set; } = string.Empty;
    public DateTime              CompletedAt { get; set; }
}
```

---

### `Services/ChecklistStore.cs`

```csharp
using System.Collections.ObjectModel;
using CheckListApp.Models;

namespace CheckListApp.Services;

public static class ChecklistStore
{
    private static ChecklistDatabase? _db;

    public static ObservableCollection<Checklist>      Checklists { get; } = new();
    public static ObservableCollection<ArchivedAction> Archive    { get; } = new();

    public static async Task InitAsync(string dbPath)
    {
        _db = new ChecklistDatabase(dbPath);

        if (await _db.IsEmptyAsync())
            await SeedAsync();

        Checklists.Clear();
        foreach (var c in await _db.GetAllChecklistsAsync())
            Checklists.Add(c);

        Archive.Clear();
        foreach (var a in await _db.GetArchiveAsync())
            Archive.Add(a);
    }

    public static async Task AddChecklistAsync(Checklist checklist)
    {
        Checklists.Add(checklist);
        if (_db is not null) await _db.SaveChecklistAsync(checklist);
    }

    public static async Task SaveChecklistAsync(Checklist checklist)
    {
        if (_db is not null) await _db.SaveChecklistAsync(checklist);
    }

    public static async Task DeleteChecklistAsync(Checklist checklist)
    {
        Checklists.Remove(checklist);
        if (_db is not null) await _db.DeleteChecklistAsync(checklist.Id);
    }

    /// <summary>
    /// Пишет действие в архив, но НЕ удаляет его из шаблона.
    /// Позволяет выполнять действия повторно.
    /// </summary>
    public static async Task ArchiveAsync(Checklist checklist, ChecklistItem item)
    {
        var archived = new ArchivedAction
        {
            ChecklistId    = checklist.Id,
            ChecklistName  = checklist.Name,
            Text           = item.Text,
            AccentColorHex = checklist.AccentColorHex,
            CompletedAt    = DateTime.Now
        };

        Archive.Insert(0, archived);

        if (_db is not null)
            await _db.SaveArchivedAsync(archived);
    }

    public static async Task RestoreAsync(ArchivedAction action)
    {
        var checklist = Checklists.FirstOrDefault(c => c.Id == action.ChecklistId);
        if (checklist is null) return;

        checklist.Items.Add(new ChecklistItem { Text = action.Text });
        Archive.Remove(action);

        if (_db is not null)
        {
            await _db.SaveChecklistAsync(checklist);
            await _db.DeleteArchivedAsync(action.Id);
        }
    }

    public static async Task DeleteArchivedAsync(ArchivedAction action)
    {
        Archive.Remove(action);
        if (_db is not null) await _db.DeleteArchivedAsync(action.Id);
    }

    public static async Task ClearArchiveAsync()
    {
        Archive.Clear();
        if (_db is not null) await _db.ClearArchiveAsync();
    }

    private static async Task SeedAsync()
    {
        var seeds = new[]
        {
            Build("Покупки", "Продукты на неделю", "#512BD4",
                "Молоко", "Хлеб", "Яйца", "Сыр"),

            Build("Учёба", "Курсы .NET MAUI", "#D600AA",
                "Прочитать главу 3", "Сделать ДЗ", "Посмотреть вебинар"),

            Build("Дом", "Бытовые дела", "#2B0B98",
                "Помыть посуду", "Пропылесосить"),

            Build("Работа", "Спринт", "#ac99ea",
                "Сдать отчёт", "Созвон с командой")
        };

        foreach (var c in seeds)
            await _db!.SaveChecklistAsync(c);
    }

    private static Checklist Build(string name, string desc, string colorHex, params string[] items)
    {
        var c = new Checklist { Name = name, Description = desc, AccentColorHex = colorHex };
        foreach (var t in items)
            c.Items.Add(new ChecklistItem { Text = t });
        return c;
    }
}
```

---

### `PageModels/MainPageModel.cs`

```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CheckListApp.Models;
using CheckListApp.Services;

namespace CheckListApp.PageModels;

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

    public bool   IsTemplatesView => SelectedFilter == "Шаблонные";
    public bool   IsArchiveView   => SelectedFilter == "Архив действий";
    public string HeaderTitle     => IsTemplatesView ? "My Checklists" : "Архив действий";

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
            foreach (var c in ChecklistStore.Checklists) Templates.Add(c);
        }
        else
        {
            Archive.Clear();
            foreach (var a in ChecklistStore.Archive) Archive.Add(a);
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
```

---

### `PageModels/ChecklistDetailPageModel.cs`

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CheckListApp.Models;
using CheckListApp.Services;

namespace CheckListApp.PageModels;

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

    public async Task ArchiveItemAsync(ChecklistItem? item)
    {
        if (Checklist is null || item is null) return;
        await ChecklistStore.ArchiveAsync(Checklist, item);
    }

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
```

---

### `PageModels/ChecklistEditPageModel.cs`

```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CheckListApp.Models;
using CheckListApp.Services;

namespace CheckListApp.PageModels;

public class ChecklistEditPageModel : INotifyPropertyChanged
{
    private Checklist? _checklist;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _selectedColorName = "Фиолетовый";

    public string? ChecklistId { get; private set; }
    public bool    IsNew       => string.IsNullOrEmpty(ChecklistId);
    public string  PageTitle   => IsNew ? "Новый чек-лист" : "Редактировать";

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
```

---

### `Controls/ChecklistCardView.xaml`

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<ContentView
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:models="clr-namespace:CheckListApp.Models"
    x:Class="CheckListApp.Controls.ChecklistCardView"
    x:DataType="models:Checklist">

    <Border
        StrokeThickness="1"
        StrokeShape="RoundRectangle 12"
        Stroke="{AppThemeBinding Light={StaticResource Gray300}, Dark={StaticResource Gray600}}"
        BackgroundColor="{AppThemeBinding Light={StaticResource White}, Dark={StaticResource DarkSecondaryBackground}}"
        Padding="15">

        <Grid RowDefinitions="Auto,Auto,*" RowSpacing="10">

            <BoxView Grid.Row="0"
                     HeightRequest="4"
                     WidthRequest="40"
                     HorizontalOptions="Start"
                     CornerRadius="2"
                     Color="{Binding AccentColor}" />

            <Label Grid.Row="1"
                   Text="{Binding Name}"
                   FontSize="16"
                   FontAttributes="Bold"
                   LineBreakMode="TailTruncation"
                   TextColor="{AppThemeBinding Light={StaticResource DarkOnLightBackground}, Dark={StaticResource LightOnDarkBackground}}" />

            <Label Grid.Row="2"
                   Text="{Binding Description}"
                   FontSize="12"
                   LineBreakMode="TailTruncation"
                   MaxLines="3"
                   VerticalOptions="Start"
                   TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource LightOnDarkBackground}}" />
        </Grid>
    </Border>
</ContentView>
```

---

### `Controls/ChecklistCardView.xaml.cs`

```csharp
namespace CheckListApp.Controls;

public partial class ChecklistCardView : ContentView
{
    public ChecklistCardView()
    {
        InitializeComponent();
    }
}
```

---

### `Pages/MainPage.xaml`

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<ContentPage
    x:Class="CheckListApp.Pages.MainPage"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:controls="clr-namespace:CheckListApp.Controls"
    xmlns:models="clr-namespace:CheckListApp.Models"
    xmlns:pageModels="clr-namespace:CheckListApp.PageModels"
    x:DataType="pageModels:MainPageModel"
    x:Name="RootPage"
    Title="CheckList">

    <ContentPage.ToolbarItems>
        <ToolbarItem Text="+"
                     Command="{Binding AddChecklistCommand}"
                     IsEnabled="{Binding IsTemplatesView}" />
        <ToolbarItem Text="🗑"
                     Command="{Binding ClearArchiveCommand}"
                     IsEnabled="{Binding IsArchiveView}" />
    </ContentPage.ToolbarItems>

    <Grid RowDefinitions="Auto,Auto,Auto,*" RowSpacing="15" Padding="15">

        <!-- Фильтр в левом верхнем углу -->
        <Border Grid.Row="0"
                HorizontalOptions="Start"
                VerticalOptions="Start"
                StrokeThickness="1"
                StrokeShape="RoundRectangle 8"
                Stroke="{AppThemeBinding Light={StaticResource Gray300}, Dark={StaticResource Gray600}}"
                BackgroundColor="{AppThemeBinding Light={StaticResource LightSecondaryBackground}, Dark={StaticResource DarkSecondaryBackground}}"
                Padding="12,8,8,8"
                MinimumHeightRequest="52">

            <Grid ColumnDefinitions="*,Auto" ColumnSpacing="4">
                <Button Grid.Column="0"
                        Text="{Binding SelectedFilter}"
                        FontSize="14"
                        BackgroundColor="Transparent"
                        TextColor="{AppThemeBinding Light={StaticResource DarkOnLightBackground}, Dark={StaticResource LightOnDarkBackground}}"
                        HorizontalOptions="Start"
                        VerticalOptions="Center"
                        Clicked="OnFilterClicked" />

                <Label Grid.Column="1"
                       Text="▾"
                       FontSize="18"
                       VerticalOptions="Center"
                       InputTransparent="True"
                       TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource LightOnDarkBackground}}" />
            </Grid>
        </Border>

        <Label Grid.Row="1"
               Text="Добрый день 👋"
               FontSize="14"
               TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource LightOnDarkBackground}}" />

        <Label Grid.Row="2"
               Text="{Binding HeaderTitle}"
               FontSize="26"
               FontAttributes="Bold"
               SemanticProperties.HeadingLevel="Level1"
               TextColor="{AppThemeBinding Light={StaticResource DarkOnLightBackground}, Dark={StaticResource LightOnDarkBackground}}" />

        <!-- Шаблоны -->
        <CollectionView Grid.Row="3"
                        x:Name="ChecklistsCollectionView"
                        IsVisible="{Binding IsTemplatesView}"
                        ItemsSource="{Binding Templates}"
                        Margin="-7.5,0"
                        MinimumHeightRequest="250"
                        SelectionMode="Single"
                        SelectionChanged="OnChecklistSelected">

            <CollectionView.ItemsLayout>
                <LinearItemsLayout Orientation="Horizontal" ItemSpacing="7.5" />
            </CollectionView.ItemsLayout>

            <CollectionView.ItemTemplate>
                <DataTemplate x:DataType="models:Checklist">
                    <SwipeView>
                        <SwipeView.BottomItems>
                            <SwipeItems Mode="Reveal" SwipeBehaviorOnInvoked="Close">
                                <SwipeItem Text="Изменить"
                                           BackgroundColor="{StaticResource Primary}"
                                           Command="{Binding Path=BindingContext.EditChecklistCommand, Source={x:Reference RootPage}, x:DataType=pageModels:MainPageModel}"
                                           CommandParameter="{Binding .}" />
                                <SwipeItem Text="Удалить"
                                           BackgroundColor="{StaticResource Magenta}"
                                           Command="{Binding Path=BindingContext.DeleteChecklistCommand, Source={x:Reference RootPage}, x:DataType=pageModels:MainPageModel}"
                                           CommandParameter="{Binding .}" />
                            </SwipeItems>
                        </SwipeView.BottomItems>

                        <controls:ChecklistCardView WidthRequest="200" />
                    </SwipeView>
                </DataTemplate>
            </CollectionView.ItemTemplate>

            <CollectionView.EmptyView>
                <Label Text="Нет шаблонов. Нажмите «+» чтобы создать."
                       FontSize="14"
                       HorizontalOptions="Center"
                       VerticalOptions="Center"
                       TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource LightOnDarkBackground}}" />
            </CollectionView.EmptyView>
        </CollectionView>

        <!-- Архив -->
        <CollectionView Grid.Row="3"
                        IsVisible="{Binding IsArchiveView}"
                        ItemsSource="{Binding Archive}"
                        SelectionMode="None">

            <CollectionView.ItemsLayout>
                <LinearItemsLayout Orientation="Vertical" ItemSpacing="8" />
            </CollectionView.ItemsLayout>

            <CollectionView.ItemTemplate>
                <DataTemplate x:DataType="models:ArchivedAction">
                    <SwipeView>
                        <SwipeView.RightItems>
                            <SwipeItems Mode="Reveal" SwipeBehaviorOnInvoked="Close">
                                <SwipeItem Text="Вернуть"
                                           BackgroundColor="{StaticResource Primary}"
                                           Command="{Binding Path=BindingContext.RestoreActionCommand, Source={x:Reference RootPage}, x:DataType=pageModels:MainPageModel}"
                                           CommandParameter="{Binding .}" />
                                <SwipeItem Text="Удалить"
                                           BackgroundColor="{StaticResource Magenta}"
                                           Command="{Binding Path=BindingContext.DeleteArchiveCommand, Source={x:Reference RootPage}, x:DataType=pageModels:MainPageModel}"
                                           CommandParameter="{Binding .}" />
                            </SwipeItems>
                        </SwipeView.RightItems>

                        <Border StrokeThickness="1"
                                StrokeShape="RoundRectangle 10"
                                Stroke="{AppThemeBinding Light={StaticResource Gray300}, Dark={StaticResource Gray600}}"
                                BackgroundColor="{AppThemeBinding Light={StaticResource White}, Dark={StaticResource DarkSecondaryBackground}}"
                                Padding="12,10">

                            <Grid ColumnDefinitions="Auto,*,Auto" ColumnSpacing="12">
                                <BoxView Grid.Column="0"
                                         WidthRequest="10" HeightRequest="10"
                                         CornerRadius="5"
                                         VerticalOptions="Center"
                                         Color="{Binding AccentColor}" />

                                <VerticalStackLayout Grid.Column="1" Spacing="2">
                                    <Label Text="{Binding Text}"
                                           FontSize="14"
                                           LineBreakMode="TailTruncation"
                                           TextColor="{AppThemeBinding Light={StaticResource DarkOnLightBackground}, Dark={StaticResource LightOnDarkBackground}}" />
                                    <Label FontSize="11"
                                           TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource LightOnDarkBackground}}">
                                        <Label.FormattedText>
                                            <FormattedString>
                                                <Span Text="✓ из " />
                                                <Span Text="{Binding ChecklistName}" FontAttributes="Italic" />
                                            </FormattedString>
                                        </Label.FormattedText>
                                    </Label>
                                </VerticalStackLayout>

                                <Label Grid.Column="2"
                                       Text="{Binding CompletedAtText}"
                                       FontSize="11"
                                       VerticalOptions="Center"
                                       TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource LightOnDarkBackground}}" />
                            </Grid>
                        </Border>
                    </SwipeView>
                </DataTemplate>
            </CollectionView.ItemTemplate>

            <CollectionView.EmptyView>
                <Label Text="Архив пуст. Отметьте действия в шаблонах."
                       FontSize="14"
                       HorizontalOptions="Center"
                       VerticalOptions="Center"
                       TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource LightOnDarkBackground}}" />
            </CollectionView.EmptyView>
        </CollectionView>
    </Grid>
</ContentPage>
```

---

### `Pages/MainPage.xaml.cs`

```csharp
using CheckListApp.Models;
using CheckListApp.PageModels;

namespace CheckListApp.Pages;

public partial class MainPage : ContentPage
{
    private readonly MainPageModel _model = new();

    public MainPage()
    {
        InitializeComponent();
        BindingContext = _model;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _model.InitAsync();
            _model.RefreshView();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка инициализации", ex.Message, "OK");
        }
    }

    private async void OnFilterClicked(object sender, EventArgs e)
    {
        var options = _model.FilterOptions.ToArray();
        var choice = await DisplayActionSheet("Режим", "Отмена", null, options);

        if (choice is not null && _model.FilterOptions.Contains(choice))
            _model.SelectedFilter = choice;
    }

    private async void OnChecklistSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Checklist checklist)
            return;

        await Shell.Current.GoToAsync($"checklist?id={checklist.Id}");
        ((CollectionView)sender).SelectedItem = null;
    }
}
```

---

### `Pages/ChecklistDetailPage.xaml`

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<ContentPage
    x:Class="CheckListApp.Pages.ChecklistDetailPage"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:models="clr-namespace:CheckListApp.Models"
    xmlns:pageModels="clr-namespace:CheckListApp.PageModels"
    x:DataType="pageModels:ChecklistDetailPageModel"
    x:Name="DetailPage"
    Title="{Binding Checklist.Name}">

    <ContentPage.ToolbarItems>
        <ToolbarItem Text="✏" Clicked="OnEditClicked" />
    </ContentPage.ToolbarItems>

    <Grid RowDefinitions="Auto,*,Auto" RowSpacing="15" Padding="15">

        <VerticalStackLayout Grid.Row="0" Spacing="4">
            <Label Text="{Binding Checklist.Name}"
                   FontSize="24"
                   FontAttributes="Bold"
                   SemanticProperties.HeadingLevel="Level1"
                   TextColor="{AppThemeBinding Light={StaticResource DarkOnLightBackground}, Dark={StaticResource LightOnDarkBackground}}" />
            <Label Text="{Binding Checklist.Description}"
                   FontSize="13"
                   TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource LightOnDarkBackground}}" />
        </VerticalStackLayout>

        <CollectionView Grid.Row="1"
                        ItemsSource="{Binding Checklist.Items}"
                        SelectionMode="None">

            <CollectionView.ItemTemplate>
                <DataTemplate x:DataType="models:ChecklistItem">
                    <SwipeView>
                        <SwipeView.RightItems>
                            <SwipeItems Mode="Reveal" SwipeBehaviorOnInvoked="Close">
                                <SwipeItem Text="Удалить"
                                           BackgroundColor="{StaticResource Magenta}"
                                           Command="{Binding Path=BindingContext.RemoveItemCommand, Source={x:Reference DetailPage}, x:DataType=pageModels:ChecklistDetailPageModel}"
                                           CommandParameter="{Binding .}" />
                            </SwipeItems>
                        </SwipeView.RightItems>

                        <Grid Padding="0,6" ColumnDefinitions="Auto,*,Auto" ColumnSpacing="12">
                            <Grid.GestureRecognizers>
                                <TapGestureRecognizer Tapped="OnItemTapped" />
                            </Grid.GestureRecognizers>

                            <Border Grid.Column="0"
                                    WidthRequest="26" HeightRequest="26"
                                    StrokeThickness="2"
                                    StrokeShape="RoundRectangle 6"
                                    VerticalOptions="Center"
                                    Stroke="{AppThemeBinding Light={StaticResource Gray400}, Dark={StaticResource Gray500}}"
                                    BackgroundColor="Transparent">
                                <Border.Triggers>
                                    <DataTrigger TargetType="Border" Binding="{Binding IsDone}" Value="True">
                                        <Setter Property="BackgroundColor" Value="{StaticResource Primary}" />
                                        <Setter Property="Stroke" Value="{StaticResource Primary}" />
                                    </DataTrigger>
                                </Border.Triggers>

                                <Label Text="✓"
                                       FontSize="16"
                                       FontAttributes="Bold"
                                       TextColor="White"
                                       HorizontalOptions="Center"
                                       VerticalOptions="Center"
                                       IsVisible="{Binding IsDone}" />
                            </Border>

                            <Label Grid.Column="1"
                                   Text="{Binding Text}"
                                   FontSize="15"
                                   VerticalOptions="Center"
                                   TextColor="{AppThemeBinding Light={StaticResource DarkOnLightBackground}, Dark={StaticResource LightOnDarkBackground}}">
                                <Label.Triggers>
                                    <DataTrigger TargetType="Label" Binding="{Binding IsDone}" Value="True">
                                        <Setter Property="TextDecorations" Value="Strikethrough" />
                                        <Setter Property="TextColor" Value="{StaticResource Gray400}" />
                                    </DataTrigger>
                                </Label.Triggers>
                            </Label>

                            <Label Grid.Column="2"
                                   Text="→"
                                   FontSize="14"
                                   VerticalOptions="Center"
                                   TextColor="{StaticResource Gray400}" />
                        </Grid>
                    </SwipeView>
                </DataTemplate>
            </CollectionView.ItemTemplate>

            <CollectionView.EmptyView>
                <VerticalStackLayout VerticalOptions="Center" Spacing="8" Padding="20">
                    <Label Text="В шаблоне пока нет действий"
                           FontSize="16"
                           HorizontalTextAlignment="Center"
                           TextColor="{AppThemeBinding Light={StaticResource DarkOnLightBackground}, Dark={StaticResource LightOnDarkBackground}}" />
                    <Label Text="Добавьте первый пункт ниже."
                           FontSize="13"
                           HorizontalTextAlignment="Center"
                           TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource LightOnDarkBackground}}" />
                </VerticalStackLayout>
            </CollectionView.EmptyView>
        </CollectionView>

        <Grid Grid.Row="2" ColumnDefinitions="*,Auto" ColumnSpacing="10">
            <Entry Grid.Column="0"
                   Text="{Binding NewItemText}"
                   Placeholder="Новое действие…"
                   FontSize="15"
                   TextColor="{AppThemeBinding Light={StaticResource DarkOnLightBackground}, Dark={StaticResource LightOnDarkBackground}}"
                   PlaceholderColor="{StaticResource Gray400}" />

            <Button Grid.Column="1"
                    Text="Добавить"
                    Command="{Binding AddItemCommand}"
                    BackgroundColor="{StaticResource Primary}"
                    TextColor="White"
                    CornerRadius="8"
                    FontSize="13"
                    Padding="14,0" />
        </Grid>
    </Grid>
</ContentPage>
```

---

### `Pages/ChecklistDetailPage.xaml.cs`

```csharp
using CheckListApp.Models;
using CheckListApp.PageModels;

namespace CheckListApp.Pages;

[QueryProperty(nameof(ChecklistId), "id")]
public partial class ChecklistDetailPage : ContentPage
{
    private readonly ChecklistDetailPageModel _model = new();

    public string? ChecklistId { get; set; }

    public ChecklistDetailPage()
    {
        InitializeComponent();
        BindingContext = _model;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (!string.IsNullOrEmpty(ChecklistId))
            _model.LoadChecklist(ChecklistId);
    }

    /// <summary>
    /// При закрытии страницы сбрасываем визуальные галочки.
    /// В БД уже всё записано, шаблон (Items) не менялся.
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_model.Checklist is not null)
        {
            foreach (var item in _model.Checklist.Items)
                item.IsDone = false;
        }
    }

    /// <summary>
    /// Тап: ставим ✓ + strike-through, пишем в архив.
    /// Галочка НЕ сбрасывается — пользователь видит, что сделано,
    /// пока страница открыта.
    /// </summary>
    private async void OnItemTapped(object sender, TappedEventArgs e)
    {
        if (sender is not Grid grid || grid.BindingContext is not ChecklistItem item)
            return;

        if (item.IsDone) return;

        item.IsDone = true;
        await _model.ArchiveItemAsync(item);
    }

    private async void OnEditClicked(object sender, EventArgs e)
    {
        if (_model.Checklist is null) return;
        await Shell.Current.GoToAsync($"checklist-edit?id={_model.Checklist.Id}");
    }
}
```

---

### `Pages/ChecklistEditPage.xaml`

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<ContentPage
    x:Class="CheckListApp.Pages.ChecklistEditPage"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:pageModels="clr-namespace:CheckListApp.PageModels"
    x:DataType="pageModels:ChecklistEditPageModel"
    Title="{Binding PageTitle}">

    <ContentPage.ToolbarItems>
        <ToolbarItem Text="Отмена" Command="{Binding CancelCommand}" />
    </ContentPage.ToolbarItems>

    <ScrollView>
        <VerticalStackLayout Padding="15" Spacing="15">

            <Label Text="Название"
                   FontSize="12"
                   TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource LightOnDarkBackground}}" />

            <Entry Text="{Binding Name}"
                   Placeholder="Например, «Отпуск»"
                   FontSize="17"
                   TextColor="{AppThemeBinding Light={StaticResource DarkOnLightBackground}, Dark={StaticResource LightOnDarkBackground}}"
                   PlaceholderColor="{StaticResource Gray400}" />

            <Label Text="Описание"
                   FontSize="12"
                   TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource LightOnDarkBackground}}" />

            <Editor Text="{Binding Description}"
                    Placeholder="Необязательно"
                    AutoSize="TextChanges"
                    HeightRequest="80"
                    FontSize="14"
                    TextColor="{AppThemeBinding Light={StaticResource DarkOnLightBackground}, Dark={StaticResource LightOnDarkBackground}}"
                    PlaceholderColor="{StaticResource Gray400}" />

            <Label Text="Цвет акцента"
                   FontSize="12"
                   TextColor="{AppThemeBinding Light={StaticResource Gray500}, Dark={StaticResource LightOnDarkBackground}}" />

            <Picker ItemsSource="{Binding ColorOptions}"
                    SelectedItem="{Binding SelectedColorName, Mode=TwoWay}"
                    FontSize="15"
                    TextColor="{AppThemeBinding Light={StaticResource DarkOnLightBackground}, Dark={StaticResource LightOnDarkBackground}}" />

            <Border StrokeThickness="0"
                    StrokeShape="RoundRectangle 12"
                    HeightRequest="60"
                    BackgroundColor="{Binding AccentColorHex}" />

            <Button Text="Сохранить"
                    Command="{Binding SaveCommand}"
                    BackgroundColor="{StaticResource Primary}"
                    TextColor="White"
                    CornerRadius="8"
                    HeightRequest="48"
                    Margin="0,20,0,0" />
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

---

### `Pages/ChecklistEditPage.xaml.cs`

```csharp
using CheckListApp.PageModels;

namespace CheckListApp.Pages;

[QueryProperty(nameof(ChecklistId), "id")]
public partial class ChecklistEditPage : ContentPage
{
    private readonly ChecklistEditPageModel _model = new();

    public string? ChecklistId { get; set; }

    public ChecklistEditPage()
    {
        InitializeComponent();
        BindingContext = _model;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _model.LoadOrCreate(ChecklistId);
    }
}
```

---

### `AppShell.xaml`

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<Shell
    x:Class="CheckListApp.AppShell"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:pages="clr-namespace:CheckListApp.Pages"
    Shell.FlyoutBehavior="Disabled"
    Title="CheckListApp">

    <ShellContent
        Title="CheckList"
        ContentTemplate="{DataTemplate pages:MainPage}"
        Route="main" />

</Shell>
```

> 💡 `Shell.FlyoutBehavior="Disabled"` — потому что основная навигация идёт через `GoToAsync("checklist?id=...")`, а не через Flyout. Если нужен Flyout с разделами — добавьте `<ShellContent>` и поставьте `FlyoutBehavior="Flyout"`.

---

### `AppShell.xaml.cs`

```csharp
using CheckListApp.Pages;

namespace CheckListApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("checklist",      typeof(ChecklistDetailPage));
        Routing.RegisterRoute("checklist-edit", typeof(ChecklistEditPage));
    }
}
```

---

### `App.xaml.cs`

```csharp
namespace CheckListApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
```

---

### `Resources/Styles/Colors.xaml` (дополнения)

В шаблонный `Colors.xaml` **добавьте в конец** (перед `</ResourceDictionary>`):

```xml
    <!-- ▼ Кастомные цвета CheckListApp -->
    <Color x:Key="DarkBackground">#17171a</Color>
    <Color x:Key="LightOnDarkBackground">#C3C3C3</Color>
    <Color x:Key="LightBackground">#F2F2F2</Color>
    <Color x:Key="DarkOnLightBackground">#0D0D0D</Color>
    <Color x:Key="LightSecondaryBackground">#E0E0E0</Color>
    <Color x:Key="DarkSecondaryBackground">#222228</Color>
```

Стандартные ключи (`Primary`, `Magenta`, `Gray100…950`, `White`, `Black`) шаблон уже содержит.

---

### `Platforms/Android/AndroidManifest.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    <application
        android:allowBackup="true"
        android:icon="@mipmap/appicon"
        android:roundIcon="@mipmap/appicon_round"
        android:supportsRtl="true" />
    <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
    <uses-permission android:name="android.permission.INTERNET" />
</manifest>
```

> ⚠️ Если ошибка `mipmap/appicon not found` — удалите `android:icon` и `android:roundIcon` (см. раздел Troubleshooting).

---

## ⚠️ Troubleshooting — ошибки и решения

### 1. `resource mipmap/appicon not found`

**Причина:** `AndroidManifest.xml` ссылается на иконку, которая не сгенерирована.

**Решение:**
- Убедиться, что в `.csproj` есть:
  ```xml
  <ItemGroup>
    <MauiIcon Include="Resources\AppIcon\appicon.svg"
              ForegroundFile="Resources\AppIcon\appiconfg.svg"
              Color="#512BD4" />
  </ItemGroup>
  ```
- Файлы `appicon.svg` и `appiconfg.svg` должны быть в `Resources/AppIcon/`.
- **Не копировать** `Resources/AppIcon/` из старого проекта.

**Быстрый обход:** убрать `android:icon` и `android:roundIcon` из `AndroidManifest.xml`.

---

### 2. `Повторяющийся атрибут "Activity"`

**Причина:** `MainActivity.cs` существует в двух местах.

**Решение:** `MainActivity.cs` только в `Platforms/Android/`. Удалить дубликат (например, из `Resources/Styles/`).

---

### 3. `ActivityAttribute` / `Bundle` не найдены

**Причина:** нет `using` в `MainActivity.cs`.

**Решение:** в `Platforms/Android/MainActivity.cs` должны быть:
```csharp
using Android.App;
using Android.Content.PM;
using Android.OS;
```

---

### 4. `CS0234 "Models"/"PageModels"/"Services" не существует`

**Причина:** папки физически не созданы или `namespace` в файлах не совпадает с именем проекта.

**Решение:**
- Проверить первую строку в каждом `.cs`: `namespace <ИмяПроекта>.Models;`
- Создать папку через **Обозреватель решений → правый клик по проекту → Создать папку**.
- Файлы должны быть **включены в проект** (не серые).

---

### 5. `CS0103 InitializeComponent не существует`

**Причина:** `.xaml`-файл не компилируется. Возможные варианты:
1. `.xaml` физически отсутствует.
2. `x:Class="X.Pages.MainPage"` ≠ `namespace X.Pages; class MainPage` (старое имя проекта).
3. `public class` без `partial`.
4. В XAML есть `XFC****` ошибка (незакрытый тег, StaticResource not found).

**Решение:**
- **Сначала** ищите в «Списке ошибок» строки с `XFC****` / `XLS****` — это настоящая причина.
- Проверить `x:Class` = `namespace` + `class`.
- Убедиться, что класс `public partial`.
- Удалить `bin`/`obj` и пересобрать.

---

### 6. `java.exe завершилась с кодом 1`

**Причина:** D8-компилятор не может обработать `.aar`. Самая частая — **устаревшая версия `Syncfusion.Maui.Toolkit`**.

**Решение:**
- `Syncfusion.Maui.Toolkit ≥ 1.0.8` для .NET 10.
- Включить подробный лог MSBuild: `Средства → Параметры → Проекты и решения → Сборка и запуск → Подробно`.
- JDK должен быть 21: `C:\Program Files\Android\openjdk\jdk-21.x.x`.

---

### 7. `CS1061 AddDebug не найден`

**Причина:** нет `Microsoft.Extensions.Logging.Debug`.

**Решение:** добавить `<PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="10.0.0" />` или использовать шаблон MAUI (там уже есть).

---

### 8. `Cannot resolve type "pageModels:ChecklistDetailPageModel"` (XAML)

**Причина:** `namespace` в `.cs` ≠ `xmlns` в XAML.

**Решение:**
- `Ctrl+Shift+F` → найти `CheckListApp` (или старое имя) → заменить через `Ctrl+Shift+H`.
- Проверить XAML-файлы вручную — VS иногда пропускает `x:Class` и `xmlns`.
- Удалить `bin`/`obj` (XAML-компилятор кэширует результаты).

---

### 9. «Фильтр не отображается»

**Причина:** `Picker` внутри `Border` без `HeightRequest` на Android схлопывается.

**Решение:** использовать `Button` + `DisplayActionSheet` вместо `Picker` — надёжнее на всех платформах:

```xml
<Button Text="{Binding SelectedFilter}" Clicked="OnFilterClicked" />
```
```csharp
private async void OnFilterClicked(object sender, EventArgs e)
{
    var choice = await DisplayActionSheet("Режим", "Отмена", null, _model.FilterOptions.ToArray());
    if (choice is not null) _model.SelectedFilter = choice;
}
```

---

### 10. `Event handler 'OnXClicked' with correct signature not found`

**Причина:** в XAML указан `Clicked="OnXClicked"`, но в `.xaml.cs` нет метода с правильной сигнатурой.

**Правильные сигнатуры:**
```csharp
private void OnXClicked(object sender, EventArgs e)                  // Button.Clicked
private void OnXSelected(object sender, SelectionChangedEventArgs e) // CollectionView.SelectionChanged
private void OnXTapped(object sender, TappedEventArgs e)             // TapGestureRecognizer
```

**Проверка:** `Ctrl+Shift+F` по имени хендлера → должно быть **2** совпадения (XAML + .cs).

---

### 11. «Перед запуском проекта его необходимо развернуть»

**Причина:** в «Диспетчере конфигурации решения» не отмечен Deploy.

**Решение:** правый клик по **решению** → «Диспетчер конфигурации» → галочка **«Развернуть»** для проекта.

---

### 12. «Сбой при развертывании на Подсистему Windows для Android (WSA)»

**Причина:** Visual Studio не может подключиться к WSA через ADB.

**Решение:**
1. Открыть «Подсистема Windows для Android» → «Дополнительные параметры» → включить **«Режим разработчика»**.
2. Запомнить IP:порт (обычно `127.0.0.1:58526`).
3. В терминале `platform-tools`:
   ```
   adb connect 127.0.0.1:58526
   adb devices
   ```
4. На экране WSA нажать «Разрешить» при первом подключении.

**Альтернатива:** использовать стандартный Android-эмулятор из «Диспетчера устройств Android».

---

### 13. `System.IO.IOException: файл ... занят другим процессом`

**Причина:** зависший процесс от предыдущей сборки удерживает файлы в `obj/`.

**Решение:**
1. Закрыть VS.
2. В диспетчере задач завершить `adb.exe`, `MSBuild.exe`, `java.exe`, `devenv.exe`.
3. Удалить `bin` и `obj`.
4. Опционально отключить параллельную сборку: `Средства → Параметры → Проекты и решения → Сборка и запуск → Максимум параллельных сборок = 1`.

---

## ✅ Чек-лист для нового MAUI-проекта

### Перед началом

- [ ] Создать проект **через шаблон .NET MAUI App**, не вручную
- [ ] F5 на пустом шаблоне — убедиться, что «Hello, World!» запускается
- [ ] Установить `sqlite-net-pcl` (последняя версия)
- [ ] Установить `Syncfusion.Maui.Toolkit ≥ 1.0.8` (если нужен Syncfusion)
- [ ] Проверить JDK (`Средства → Параметры → .NET MAUI → Android → JDK Location` → JDK 21)

### При создании файлов

- [ ] Папки создаются через **Обозреватель решений** (не в проводнике)
- [ ] `namespace` = `<ИмяПроекта>.<Папка>` (`CheckListApp.Models`)
- [ ] `x:Class` в XAML = `<ИмяПроекта>.<Папка>.<ИмяКласса>` — совпадает с `namespace` + `class`
- [ ] `class` в `.xaml.cs` — `public partial class`
- [ ] `xmlns:models` и др. — `clr-namespace:<ИмяПроекта>.<Папка>`

### После каждой новой папки

- [ ] `Ctrl+Shift+B` → 0 ошибок
- [ ] Все файлы видны в Обозревателе решений (не серые)
- [ ] Если ошибка — **не переходить** к следующей папке

### Перед первым F5

- [ ] `Colors.xaml` содержит все нужные ключи (6 кастомных + стандартные)
- [ ] `AppShell.xaml.cs` содержит `Routing.RegisterRoute(...)` для новых страниц
- [ ] `AppShell.xaml` использует `<ИмяПроекта>.Pages.*`
- [ ] В «Диспетчере конфигурации решения» отмечен Deploy
- [ ] Устройство/эмулятор выбрано в верхней панели VS
- [ ] `adb devices` показывает устройство

### Если ошибка — не гадать

1. Открыть «Список ошибок» — скопировать **все** ошибки, включая `XFC****`/`XLS****`.
2. Включить подробный лог MSBuild (`Сборка и запуск → Подробно`).
3. Искать настоящую причину в **первых** строках ошибки.
4. Удалить `bin`/`obj` перед тем, как паниковать.

---

## 📋 Шпаргалка по версиям

| Компонент | Версия | Примечание |
|---|---|---|
| .NET MAUI | 10.0.120+ | `dotnet --version` |
| TargetFramework | `net10.0-android` | |
| JDK | 21 (Microsoft OpenJDK) | `C:\Program Files\Android\openjdk\jdk-21.x.x` |
| Android SDK | API 34+ | через VS Installer |
| sqlite-net-pcl | 1.11.285+ | последняя стабильная |
| Syncfusion.Maui.Toolkit | **≥ 1.0.8** | критично для .NET 10 |
| Android min SDK | 24 | `<SupportedOSPlatformVersion>24</SupportedOSPlatformVersion>` |

---

## 🎯 Три главных правила

1. **Шаблон, а не вручную.** Любой MAUI-проект начинается с `Файл → Создать → Проект → .NET MAUI App`. Ручное создание — часы на исправление.

2. **Одна папка — одна проверка.** `Ctrl+Shift+B` после каждой новой папки. Накапливание ошибок = непонятно, что сломалось.

3. **`XFC****` раньше `CS****`.** Если `CS0103 InitializeComponent` — сначала ищите ошибки с префиксом `XFC`/`XLS` в «Списке ошибок». Именно они — настоящая причина.

---

## 📚 Расширение проекта

| Задача | Что добавить |
|---|---|
| Синхронизация с сервером | `HttpClient` + `System.Text.Json` |
| Тёмная тема вручную | `Application.Current.UserAppTheme = AppTheme.Dark` |
| Поиск по шаблонам | `SearchBar` + фильтрация в `MainPageModel` |
| Сортировка архива | `CollectionView` с `IsGrouped=true`, группировка по дате |
| Экспорт/импорт JSON | `FileSaver`/`FilePicker` из `CommunityToolkit.Maui` |
| Push-уведомления | `Plugin.LocalNotification` |
| Тесты | `xUnit` + `Moq` (вынести `ChecklistStore` в интерфейс) |

---

## 📄 Лицензия

MIT — используйте свободно.