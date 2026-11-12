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