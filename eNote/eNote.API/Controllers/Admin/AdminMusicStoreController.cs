using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.ReferenceData.MusicStores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/admin/music-stores")]
public sealed class AdminMusicStoreController(IMusicStoreService service) : CoreController
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<MusicStoreDto>>> GetPaged([FromQuery] MusicStoreSearchObject search)
    {
        var result = await service.GetPagedAsync(search);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MusicStoreDto>> GetById(int id)
    {
        var dto = await service.GetByIdAsync(id);
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<MusicStoreDto>> Create([FromBody] MusicStoreRequest request)
    {
        var dto = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MusicStoreDto>> Update(int id, [FromBody] MusicStoreRequest request)
    {
        var dto = await service.UpdateAsync(id, request);
        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
