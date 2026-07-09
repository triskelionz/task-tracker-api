# Task Tracker API

A small REST API for tracking tasks through a Todo -> In Progress -> Done workflow, built with ASP.NET Core Minimal APIs and a hand-written SQL data layer (no ORM). Written to demonstrate a clean separation between HTTP, business rules, and persistence.

## Architecture

```
Program.cs      Minimal API endpoints - HTTP concerns only
Services/       Business rules: validation, status-transition rules
Repositories/   Persistence: SQLite (raw SQL) + an in-memory implementation
Models/         Plain data models
```

- **`ITaskRepository`** is the only thing `TaskService` depends on, so persistence can be swapped (SQLite for the running API, in-memory for tests) without touching business logic.
- **`SqliteTaskRepository`** uses parameterized ADO.NET SQL directly (via `Microsoft.Data.Sqlite`) rather than an ORM, so the schema and every query are explicit.
- **`TaskService`** owns the one business rule that matters here: valid status transitions (`Todo -> InProgress -> Done`, with `InProgress` able to move back to `Todo`). Invalid transitions raise `InvalidTaskStatusTransitionException`, which the API layer maps to `409 Conflict`.

## Endpoints

| Method | Route | Description |
| --- | --- | --- |
| GET | `/tasks` | List all tasks |
| GET | `/tasks/{id}` | Get a single task |
| POST | `/tasks` | Create a task (`{ "title": "...", "description": "..." }`) |
| PUT | `/tasks/{id}` | Update a task title/description |
| PATCH | `/tasks/{id}/status` | Change status (`{ "status": "InProgress" }`) |
| DELETE | `/tasks/{id}` | Delete a task |

## Running

```bash
dotnet run
```

The API listens with Swagger UI enabled in development (`/swagger`) and persists to a local `tasks.db` SQLite file, created automatically on first run.

## Running the tests

```bash
dotnet test
```

Tests exercise `TaskService` against `InMemoryTaskRepository`, so they run in-process with no database and no HTTP server.

## License

MIT

