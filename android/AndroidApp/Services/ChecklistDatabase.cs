using System.Text.Json;
using AndroidApp.Models;
using SQLite;

namespace AndroidApp.Services;

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