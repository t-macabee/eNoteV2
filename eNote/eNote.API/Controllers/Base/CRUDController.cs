using eNote.Application.Interfaces.Base;
using eNote.Application.SearchObjects;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Base
{    
    public abstract class CRUDController<TModel, TSearch, TInsert, TUpdate>(ICRUDService<TModel, TSearch, TInsert, TUpdate> service)
        : BaseController<TModel, TSearch>(service) where TModel : class where TSearch : BaseSearchObject
    {
        protected readonly ICRUDService<TModel, TSearch, TInsert, TUpdate> _crudService = service;

        [HttpPost]
        public virtual async Task<ActionResult<TModel>> Insert(
            [FromBody] TInsert request)
        {
            var result = await _crudService.InsertAsync(request);
            return Ok(result); 
        }

        [HttpPut("{id:int}")]
        public virtual async Task<ActionResult<TModel>> Update(
            int id,
            [FromBody] TUpdate request)
        {
            var result = await _crudService.UpdateAsync(id, request);
            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        public virtual async Task<IActionResult> Delete(int id)
        {
            await _crudService.DeleteAsync(id);
            return NoContent();
        }
    }
}
