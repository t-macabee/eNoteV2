using eNote.Application.Common.Crud;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Search;
using eNote.Domain.Entities.Shared.Base;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Base;

/// The five CRUD actions shared by the Admin reference-data controllers. Each
/// derived controller carries its own route and role; only [GetId] is added.
public abstract class ReferenceDataController<TEntity, TDto, TRequest, TSearch>(
    ReferenceDataCrudService<TEntity, TDto, TRequest, TSearch> service)
    : CoreController
    where TEntity : class, IEntity
    where TSearch : BaseSearchObject
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public virtual async Task<ActionResult<PagedResult<TDto>>> GetPaged([FromQuery] TSearch search, CancellationToken cancellationToken)
    {
        PagedResult<TDto> result = await service.GetPagedAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public virtual async Task<ActionResult<TDto>> GetById(int id, CancellationToken cancellationToken)
    {
        TDto dto = await service.GetByIdAsync(id, cancellationToken);
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public virtual async Task<ActionResult<TDto>> Create([FromBody] TRequest request, CancellationToken cancellationToken)
    {
        TDto dto = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = GetId(dto) }, dto);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public virtual async Task<ActionResult<TDto>> Update(int id, [FromBody] TRequest request, CancellationToken cancellationToken)
    {
        TDto dto = await service.UpdateAsync(id, request, cancellationToken);
        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public virtual async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    protected abstract int GetId(TDto dto);
}
