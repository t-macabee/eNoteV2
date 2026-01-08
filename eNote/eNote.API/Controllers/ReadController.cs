using eNote.Application;
using eNote.Application.Interfaces.Base;
using eNote.Application.SearchObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers
{
    [ApiController]
    //[Authorize]
    [Route("api/[controller]")]
    public abstract class ReadController<TModel, TSearch>(IService<TModel, TSearch> service) : ControllerBase where TSearch : BaseSearchObject
    {
        protected readonly IService<TModel, TSearch> _service = service;

        [HttpGet]
        public virtual async Task<ActionResult<PagedResult<TModel>>> GetAll(
            [FromQuery] TSearch search)
        {
            var result = await _service.GetPagedAsync(search);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public virtual async Task<ActionResult<TModel>> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return Ok(result);
        }
    }
}
