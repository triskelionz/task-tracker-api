using System.Collections.Concurrent;
using TaskTrackerApi.Models;

namespace TaskTrackerApi.Repositories;

// In-memory implementation used by unit tests so TaskService can be exercised
// without a database or an HTTP server.
public class InMemoryTaskRepository : ITaskRepository
{
    private readonly ConcurrentDictionary<int, TaskItem> _tasks = new();
    private int _nextId = 1;

    public Task<IEnumerable<TaskItem>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<TaskItem>>(_tasks.Values.OrderBy(t => t.Id).ToList());
    }

    public Task<TaskItem?> GetByIdAsync(int id)
    {
        _tasks.TryGetValue(id, out var task);
        return Task.FromResult(task);
    }

    public Task<TaskItem> CreateAsync(TaskItem task)
    {
        task.Id = Interlocked.Increment(ref _nextId) - 1;
        _tasks[task.Id] = task;
        return Task.FromResult(task);
    }

    public Task<bool> UpdateAsync(TaskItem task)
    {
        if (!_tasks.ContainsKey(task.Id))
        {
            return Task.FromResult(false);
        }

        _tasks[task.Id] = task;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(int id)
    {
        return Task.FromResult(_tasks.TryRemove(id, out _));
    }
}

