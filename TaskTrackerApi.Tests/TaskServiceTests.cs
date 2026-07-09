using TaskTrackerApi.Models;
using TaskTrackerApi.Repositories;
using TaskTrackerApi.Services;
using Xunit;

namespace TaskTrackerApi.Tests;

// Runs entirely against InMemoryTaskRepository, so no database or HTTP
// server is needed to exercise TaskService business rules.
public class TaskServiceTests
{
    private static TaskService CreateService() => new(new InMemoryTaskRepository());

    [Fact]
    public async Task CreateAsync_AddsTaskWithTodoStatus()
    {
        var service = CreateService();

        var task = await service.CreateAsync("Write tests", "Cover the happy path");

        Assert.Equal("Write tests", task.Title);
        Assert.Equal(TaskState.Todo, task.Status);
        Assert.True(task.Id > 0);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenTitleIsEmpty()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync("   ", null));
    }

    [Fact]
    public async Task ChangeStatusAsync_AllowsTodoToInProgress()
    {
        var service = CreateService();
        var task = await service.CreateAsync("Task", null);

        var result = await service.ChangeStatusAsync(task.Id, TaskState.InProgress);

        Assert.True(result);
        var updated = await service.GetByIdAsync(task.Id);
        Assert.Equal(TaskState.InProgress, updated!.Status);
    }

    [Fact]
    public async Task ChangeStatusAsync_AllowsInProgressBackToTodo()
    {
        var service = CreateService();
        var task = await service.CreateAsync("Task", null);
        await service.ChangeStatusAsync(task.Id, TaskState.InProgress);

        var result = await service.ChangeStatusAsync(task.Id, TaskState.Todo);

        Assert.True(result);
    }

    [Fact]
    public async Task ChangeStatusAsync_RejectsTodoDirectlyToDone()
    {
        var service = CreateService();
        var task = await service.CreateAsync("Task", null);

        await Assert.ThrowsAsync<InvalidTaskStatusTransitionException>(
            () => service.ChangeStatusAsync(task.Id, TaskState.Done));
    }

    [Fact]
    public async Task ChangeStatusAsync_RejectsTransitionsOutOfDone()
    {
        var service = CreateService();
        var task = await service.CreateAsync("Task", null);
        await service.ChangeStatusAsync(task.Id, TaskState.InProgress);
        await service.ChangeStatusAsync(task.Id, TaskState.Done);

        await Assert.ThrowsAsync<InvalidTaskStatusTransitionException>(
            () => service.ChangeStatusAsync(task.Id, TaskState.InProgress));
    }

    [Fact]
    public async Task ChangeStatusAsync_SameStatusIsNoOp()
    {
        var service = CreateService();
        var task = await service.CreateAsync("Task", null);

        var result = await service.ChangeStatusAsync(task.Id, TaskState.Todo);

        Assert.True(result);
    }

    [Fact]
    public async Task ChangeStatusAsync_ReturnsFalseForMissingTask()
    {
        var service = CreateService();

        var result = await service.ChangeStatusAsync(999, TaskState.InProgress);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_RemovesExistingTask()
    {
        var service = CreateService();
        var task = await service.CreateAsync("Task", null);

        var deleted = await service.DeleteAsync(task.Id);

        Assert.True(deleted);
        Assert.Null(await service.GetByIdAsync(task.Id));
    }

    [Fact]
    public async Task UpdateDetailsAsync_UpdatesTitleAndDescription()
    {
        var service = CreateService();
        var task = await service.CreateAsync("Old title", "Old description");

        var updated = await service.UpdateDetailsAsync(task.Id, "New title", "New description");

        Assert.True(updated);
        var fetched = await service.GetByIdAsync(task.Id);
        Assert.Equal("New title", fetched!.Title);
        Assert.Equal("New description", fetched.Description);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllCreatedTasks()
    {
        var service = CreateService();
        await service.CreateAsync("Task 1", null);
        await service.CreateAsync("Task 2", null);

        var tasks = await service.GetAllAsync();

        Assert.Equal(2, tasks.Count());
    }
}

