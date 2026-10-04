using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Data.ResponseModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApp.Core.Dtos;
using MyApp.Core.Entities;
using MyApp.Core.Enums;
using MyApp.Infrastructure.Repositories;
using MyApp.Server.Binding;

namespace MyApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TodosController(IRepository<Todo> repo) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<LoadResult>> GetAll(
        [FromQuery] DataSourceLoadOptions loadOptions, CancellationToken ct)
    {
        // DevExtreme protocol string matching is case-insensitive; EF SQLite translates Contains to instr() which is not.
        loadOptions.StringToLower = true;
        var query = repo.Query()
            .Select(t => new TodoDto
            {
                Id = t.Id, Title = t.Title, Status = t.Status, CreatedAt = t.CreatedAt, DueDate = t.DueDate
            });
        var result = await DataSourceLoader.LoadAsync(query, loadOptions, ct);
        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<TodoStatsDto>> GetStats(CancellationToken ct)
    {
        var query = repo.Query();
        var total = await query.CountAsync(ct);
        var completed = await query.CountAsync(t => t.Status == TodoStatus.Completed, ct);
        var pending = total - completed;
        return Ok(new TodoStatsDto { Total = total, Completed = completed, Pending = pending });
    }

    [HttpGet("week")]
    public async Task<ActionResult<WeekResponseDto>> GetWeek(CancellationToken ct)
    {
        var monday = WeekPlanner.GetWeekStart(DateOnly.FromDateTime(DateTime.UtcNow));
        var todos = await repo.Query().ToListAsync(ct);
        return Ok(WeekPlanner.BuildWeek(monday, todos));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TodoDto>> GetById(int id, CancellationToken ct)
    {
        var todo = await repo.GetByIdAsync(id, ct);
        if (todo is null) return NotFound();
        return Ok(todo.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<TodoDto>> Create(
        [FromBody] TodoCreateDto dto, CancellationToken ct)
    {
        var todo = dto.ToEntity();
        await repo.AddAsync(todo, ct);
        return CreatedAtAction(nameof(GetById), new { id = todo.Id }, todo.ToDto());
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TodoDto>> Update(
        int id, [FromBody] TodoUpdateDto dto, CancellationToken ct)
    {
        var todo = await repo.GetByIdAsync(id, ct);
        if (todo is null) return NotFound();
        var updated = todo with { Title = dto.Title, Status = dto.Status, DueDate = dto.DueDate };
        await repo.UpdateAsync(updated, ct);
        return Ok(updated.ToDto());
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await repo.DeleteAsync(id, ct);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
