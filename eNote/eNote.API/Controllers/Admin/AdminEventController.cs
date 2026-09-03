using eNote.API.Controllers.Base;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Communication.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/v{version:apiVersion}/admin/events")]
public sealed class AdminEventController(EventService service) : CoreController
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EventDto>>> GetPaged([FromQuery] EventSearchObject search, CancellationToken cancellationToken)
    {
        PagedResult<EventDto> result = await service.GetPagedAsync(search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<EventDto>> GetById(int id, CancellationToken cancellationToken)
    {
        EventDto dto = await service.GetByIdAsync(id, cancellationToken);
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EventDto>> Create([FromBody] EventRequest request, CancellationToken cancellationToken)
    {
        EnsurePlatformWide(request);

        EventDto dto = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> Update(int id, [FromBody] EventRequest request, CancellationToken cancellationToken)
    {
        EnsurePlatformWide(request);

        var existing = await service.GetByIdAsync(id, cancellationToken);
        EnsurePlatformWide(existing);

        EventDto dto = await service.UpdateAsync(id, request, cancellationToken);
        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var existing = await service.GetByIdAsync(id, cancellationToken);
        EnsurePlatformWide(existing);

        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private static void EnsurePlatformWide(EventRequest request)
    {
        if (request.CourseId.HasValue || request.InstructorId.HasValue)
        {
            throw new BusinessException(Messages.AdminEventPlatformWideOnly);
        }
    }

    private static void EnsurePlatformWide(EventDto dto)
    {
        if (dto.CourseId.HasValue || dto.InstructorId.HasValue)
        {
            throw new BusinessException(Messages.AdminEventPlatformWideOnly);
        }
    }
}
