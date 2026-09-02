using System;
using System.Linq.Expressions;
using AspNetCoreApiStarter.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreApiStarter.Controllers
{
    public class BaseController<TEntity> : ControllerBase where TEntity : class
    {
        private readonly ShopDbContext _dbContext;

        public BaseController(ShopDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public virtual async Task<ActionResult<IEnumerable<TEntity>>> Get(Expression<Func<TEntity, bool>> predicate = null, int? id = null)
        {
            var query = _dbContext.Set<TEntity>().AsQueryable();
            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (id.HasValue)
            {
                query = query.Where(e => (int)e.GetType().GetProperty("Id").GetValue(e) == id.Value);
            }

            // If id is null, return all entities
            if (!id.HasValue)
            {
                return await query.ToListAsync();
            }
            else
            {
                // Otherwise, return a single entity or NotFound if not found
                var entity = await query.SingleOrDefaultAsync();
                if (entity == null)
                {
                    return NotFound();
                }
                else
                {
                    return Ok(entity);
                }
            }
        }

        [HttpPost]
        public virtual async Task<ActionResult<TEntity>> Post(TEntity entity)
        {
            // Convert all DateTime properties to UTC before saving
            var dateTimeProps = entity.GetType()
                .GetProperties()
                .Where(p => p.PropertyType == typeof(DateTime));

            foreach (var prop in dateTimeProps)
            {
                var value = (DateTime)prop.GetValue(entity);
                if (value.Kind == DateTimeKind.Local)
                {
                    prop.SetValue(entity, value.ToUniversalTime());
                }
            }

            _dbContext.Set<TEntity>().Add(entity);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new
            {
                id = entity.GetType().GetProperty("Id").GetValue(entity)
            }, entity);
        }

        [HttpPut("{id}")]
        public virtual async Task<IActionResult> Put(int id, TEntity entity)
        {
            if (id != (int)entity.GetType().GetProperty("Id").GetValue(entity))
            {
                return BadRequest();
            }
            
            // Convert all DateTime properties to UTC before saving
            var dateTimeProps = entity.GetType()
                .GetProperties()
                .Where(p => p.PropertyType == typeof(DateTime));

            foreach (var prop in dateTimeProps)
            {
                var value = (DateTime)prop.GetValue(entity);
                if (value.Kind == DateTimeKind.Local)
                {
                    prop.SetValue(entity, value.ToUniversalTime());
                }
            }

            _dbContext.Entry(entity).State = EntityState.Modified;
            
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EntityExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public virtual async Task<IActionResult> Delete(int id)
        {
            var entity = await Get(id: id);

            if (entity == null)
            {
                return NotFound();
            }

            _dbContext.Set<TEntity>().Remove(await _dbContext.Set<TEntity>().SingleOrDefaultAsync(e => (int)e.GetType().GetProperty("Id").GetValue(e) == id));
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }


        private bool EntityExists(int id)
        {
            return _dbContext.Set<TEntity>().Any(
                e => (int)e.GetType().GetProperty("Id").GetValue(e) == id);
        }
    }
}
