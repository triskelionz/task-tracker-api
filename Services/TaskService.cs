using TaskTrackerApi.Models;
using TaskTrackerApi.Repositories;

namespace TaskTrackerApi.Services;

// Thrown when a status change does not follow the allowed workflow.
// The API layer maps this to a 409 Conflict response.
public class InvalidTaskStatusTransitionException : Exception
{
    public InvalidTaskStatusTransitionException(TaskState from, TaskState to)
        : base($"Cannot transition task from {from} to {to}.")
    {
    }
}

// Owns the one business rule that matters for this API: which status
// transitions are valid. Everything else is a pass-through to the repository.
public class TaskService
{
    private static readonly Dictionary<TaskState, TaskState[]> AllowedTransitions = new()
    {
        [TaskState.Todo] = new[] { TaskState.InProgress },
        [TaskState.InProgress] = new[] { TaskState.Todo, TaskState.Done },
        [TaskState.Done] = Array.Empty<TaskState>(),
    };

    private readonly ITaskRepository _repository;

    public TaskService(ITaskRepository repository)
    {
        _repository = repository;
    }

    public Task<IEnumerable<TaskItem>> GetAllAsync() => _repository.GetAllAsync();

    public Task<TaskItem?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

    public Task<TaskItem> CreateAsync(string title, string? description)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        var task = new TaskItem { Title = title.Trim(), Description = description };
        return _repository.CreateAsync(task);
    }

    public async Task<bool> UpdateDetailsAsync(int id, string title, string? description)
    {
        var task = await _repository.GetByIdAsync(id);
        if (task is null)
        {
            return false;
        }

        task.Title = title.Trim();
        task.Description = description;
        return await _repository.UpdateAsync(task);
    }

    public async Task<bool> ChangeStatusAsync(int id, TaskState newStatus)
    {
        var task = await _repository.GetByIdAsync(id);
        if (task is null)
        {
            return false;
        }

        if (task.Status == newStatus)
        {
            return true;
        }

        if (!AllowedTransitions[task.Status].Contains(newStatus))
        {
            throw new InvalidTaskStatusTransitionException(task.Status, newStatus);
        }

        task.Status = newStatus;
        return await _repository.UpdateAsync(task);
    }

    public Task<bool> DeleteAsync(int id) => _repository.DeleteAsync(id);
}

