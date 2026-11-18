using System.Collections.ObjectModel;
using AndroidApp.Models;

namespace AndroidApp.Services;

public static class ChecklistStore
{
    private static ChecklistDatabase? _db;

    public static ObservableCollection<Checklist>      Checklists { get; } = new();
    public static ObservableCollection<ArchivedAction> Archive    { get; } = new();

    //public static string CurrentFilter { get; private set; } = "Шаблонные";
    //public static event Action? FilterChanged;

   // public static void SetFilter(string filter)
   // {
   //     if (CurrentFilter == filter) return;
   //     CurrentFilter = filter;
   //     FilterChanged?.Invoke();
   // }

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