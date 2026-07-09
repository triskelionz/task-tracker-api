namespace TaskTrackerApi.Models;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskState Status { get; set; } = TaskState.Todo;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

