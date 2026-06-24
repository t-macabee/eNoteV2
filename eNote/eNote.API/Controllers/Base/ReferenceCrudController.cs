using eNote.Application.Common.Paging;
using eNote.Application.Common.Search;
using eNote.Application.Features.ReferenceData;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Base
{
    public abstract class ReferenceCrudController<TDto, TRequest, TSearch>(IReferenceCrudService<TDto, TRequest, TSearch> service) : CoreController
        where TSearch : BaseSearchObject
    {
        [HttpGet]
        public async Task<ActionResult<PagedResult<TDto>>> GetPaged([FromQuery] TSearch search)
        {
            PagedResult<TDto> result = await service.GetPagedAsync(search);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TDto>> GetById(int id)
        {
            TDto dto = await service.GetByIdAsync(id);
            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<TDto>> Create([FromBody] TRequest request)
        {
            TDto dto = await service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = GetDtoId(dto) }, dto);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<TDto>> Update(int id, [FromBody] TRequest request)
        {
            TDto dto = await service.UpdateAsync(id, request);
            return Ok(dto);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await service.DeleteAsync(id);
            return NoContent();
        }

        private static object GetDtoId(TDto dto) =>
            typeof(TDto).GetProperty("Id")?.GetValue(dto)
            ?? throw new InvalidOperationException($"{typeof(TDto).Name} must expose an Id property.");
    }
}
