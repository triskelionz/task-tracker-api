using Microsoft.OpenApi.Models;
using TaskTrackerApi.Models;
using TaskTrackerApi.Repositories;
using TaskTrackerApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Task Tracker API", Version = "v1" });
});

var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=tasks.db";
builder.Services.AddSingleton<ITaskRepository>(_ => new SqliteTaskRepository(connectionString));
builder.Services.AddScoped<TaskService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/tasks", async (TaskService service) => await service.GetAllAsync());

app.MapGet("/tasks/{id:int}", async (int id, TaskService service) =>
{
    var task = await service.GetByIdAsync(id);
    return task is not null ? Results.Ok(task) : Results.NotFound();
});

app.MapPost("/tasks", async (CreateTaskRequest request, TaskService service) =>
{
    try
    {
        var task = await service.CreateAsync(request.Title, request.Description);
        return Results.Created($"/tasks/{task.Id}", task);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapPut("/tasks/{id:int}", async (int id, UpdateTaskRequest request, TaskService service) =>
{
    var updated = await service.UpdateDetailsAsync(id, request.Title, request.Description);
    return updated ? Results.NoContent() : Results.NotFound();
});

app.MapPatch("/tasks/{id:int}/status", async (int id, UpdateStatusRequest request, TaskService service) =>
{
    try
    {
        var updated = await service.ChangeStatusAsync(id, request.Status);
        return updated ? Results.NoContent() : Results.NotFound();
    }
    catch (InvalidTaskStatusTransitionException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
});

app.MapDelete("/tasks/{id:int}", async (int id, TaskService service) =>
{
    var deleted = await service.DeleteAsync(id);
    return deleted ? Results.NoContent() : Results.NotFound();
});

app.Run();

record CreateTaskRequest(string Title, string? Description);
record UpdateTaskRequest(string Title, string? Description);
record UpdateStatusRequest(TaskState Status);

