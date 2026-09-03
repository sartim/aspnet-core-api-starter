using System.Linq.Expressions;
using AspNetCoreApiStarter.Data;
using AspNetCoreApiStarter.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreApiStarter.Controllers;

public class BaseController<TEntity> : ControllerBase where TEntity : class
{
    private readonly ApplicationDbContext _dbContext;

    public BaseController(ApplicationDbContext dbContext) => _dbContext = dbContext;

    [HttpGet]
    public virtual async Task<ActionResult<object>> Get([FromQuery] PageQuery query, [FromQuery] string? id = null)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            var entity = await FindEntity(id);
            return entity is null ? NotFound() : Ok(entity);
        }

        var entities = _dbContext.Set<TEntity>().AsNoTracking();
        if (!query.IncludeDeleted && typeof(TEntity).GetProperty(nameof(Base.IsDeleted)) is not null)
            entities = entities.Where(entity => !EF.Property<bool>(entity, nameof(Base.IsDeleted)));
        if (!string.IsNullOrWhiteSpace(query.Q))
            entities = ApplyTextFilter(entities, query.Q.Trim());

        var totalCount = await entities.CountAsync();
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize);
        var orderedEntities = typeof(TEntity).GetProperty(nameof(Base.CreatedAt)) is not null
            ? entities.OrderByDescending(entity => EF.Property<DateTime>(entity, nameof(Base.CreatedAt)))
            : entities.OrderByDescending(entity => EF.Property<int>(entity, "Id"));
        var items = await orderedEntities
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return Ok(new PagedResponse<TEntity>(items, query.Page, query.PageSize, totalCount, totalPages));
    }

    [HttpPost]
    public virtual async Task<ActionResult<TEntity>> Post(TEntity entity)
    {
        ConvertDateTimesToUtc(entity);
        _dbContext.Set<TEntity>().Add(entity);
        await _dbContext.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = GetEntityId(entity) }, entity);
    }

    [HttpPut("{id}")]
    public virtual async Task<IActionResult> Put(string id, TEntity entity)
    {
        if (!string.Equals(id, GetEntityId(entity), StringComparison.OrdinalIgnoreCase))
            return BadRequest(new ProblemDetails { Title = "Invalid resource id", Detail = "The route id must match the entity id." });
        if (await FindEntity(id) is null)
            return NotFound();

        ConvertDateTimesToUtc(entity);
        _dbContext.Entry(entity).State = EntityState.Modified;
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public virtual async Task<IActionResult> Delete(string id)
    {
        var entity = await FindEntity(id);
        if (entity is null)
            return NotFound();

        _dbContext.Set<TEntity>().Remove(entity);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    private async Task<TEntity?> FindEntity(string id)
    {
        var property = typeof(TEntity).GetProperty("Id");
        if (property is null)
            return null;
        object? key = property.PropertyType == typeof(Guid) && Guid.TryParse(id, out var guid) ? guid :
            property.PropertyType == typeof(int) && int.TryParse(id, out var integer) ? integer : null;
        return key is null ? null : await _dbContext.Set<TEntity>().FindAsync(key);
    }

    private static string GetEntityId(TEntity entity) =>
        entity.GetType().GetProperty("Id")?.GetValue(entity)?.ToString() ?? string.Empty;

    private static void ConvertDateTimesToUtc(TEntity entity)
    {
        foreach (var property in entity.GetType().GetProperties().Where(property => property.PropertyType == typeof(DateTime)))
        {
            var value = (DateTime)property.GetValue(entity)!;
            if (value.Kind == DateTimeKind.Local)
                property.SetValue(entity, value.ToUniversalTime());
        }
    }

    private static IQueryable<TEntity> ApplyTextFilter(IQueryable<TEntity> entities, string search)
    {
        var stringProperties = typeof(TEntity).GetProperties()
            .Where(property => property.PropertyType == typeof(string)).ToArray();
        if (stringProperties.Length == 0)
            return entities.Where(_ => false);

        var parameter = Expression.Parameter(typeof(TEntity), "entity");
        var searchExpression = Expression.Constant(search.ToLowerInvariant());
        Expression? predicate = null;
        foreach (var property in stringProperties)
        {
            var value = Expression.Call(
                Expression.Call(typeof(EF), nameof(EF.Property), [typeof(string)], parameter, Expression.Constant(property.Name)),
                nameof(string.ToLower), Type.EmptyTypes);
            var contains = Expression.Call(value, nameof(string.Contains), Type.EmptyTypes, searchExpression);
            predicate = predicate is null ? contains : Expression.OrElse(predicate, contains);
        }
        return entities.Where(Expression.Lambda<Func<TEntity, bool>>(predicate!, parameter));
    }
}
