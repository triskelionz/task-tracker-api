using Microsoft.Data.Sqlite;
using TaskTrackerApi.Models;

namespace TaskTrackerApi.Repositories;

// Uses raw parameterized ADO.NET SQL rather than an ORM, so the schema and
// every query are explicit and easy to audit.
public class SqliteTaskRepository : ITaskRepository
{
    private readonly string _connectionString;

    public SqliteTaskRepository(string connectionString)
    {
        _connectionString = connectionString;
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Tasks (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Description TEXT NULL,
                Status INTEGER NOT NULL,
                CreatedAtUtc TEXT NOT NULL
            );";
        command.ExecuteNonQuery();
    }

    public async Task<IEnumerable<TaskItem>> GetAllAsync()
    {
        var tasks = new List<TaskItem>();

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Title, Description, Status, CreatedAtUtc FROM Tasks ORDER BY Id;";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tasks.Add(ReadTask(reader));
        }

        return tasks;
    }

    public async Task<TaskItem?> GetByIdAsync(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Title, Description, Status, CreatedAtUtc FROM Tasks WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", id);

        using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadTask(reader) : null;
    }

    public async Task<TaskItem> CreateAsync(TaskItem task)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Tasks (Title, Description, Status, CreatedAtUtc)
            VALUES (@title, @description, @status, @createdAtUtc);
            SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("@title", task.Title);
        command.Parameters.AddWithValue("@description", task.Description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@status", (int)task.Status);
        command.Parameters.AddWithValue("@createdAtUtc", task.CreatedAtUtc.ToString("O"));

        var newId = (long)(await command.ExecuteScalarAsync() ?? 0L);
        task.Id = (int)newId;
        return task;
    }

    public async Task<bool> UpdateAsync(TaskItem task)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Tasks
            SET Title = @title, Description = @description, Status = @status
            WHERE Id = @id;";
        command.Parameters.AddWithValue("@title", task.Title);
        command.Parameters.AddWithValue("@description", task.Description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@status", (int)task.Status);
        command.Parameters.AddWithValue("@id", task.Id);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Tasks WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", id);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    private static TaskItem ReadTask(SqliteDataReader reader)
    {
        return new TaskItem
        {
            Id = reader.GetInt32(0),
            Title = reader.GetString(1),
            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
            Status = (TaskState)reader.GetInt32(3),
            CreatedAtUtc = DateTime.Parse(reader.GetString(4)),
        };
    }
}

