using eNote.API.Controllers.Base;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Constants;
using eNote.Application.Features.Communication.Events;
using eNote.Domain.Entities.Communication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/v{version:apiVersion}/admin/events")]
public sealed class AdminEventController(EventService service)
    : ReferenceDataController<Event, EventDto, EventRequest, EventSearchObject>(service)
{
    protected override int GetId(EventDto dto) => dto.Id;

    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public override async Task<ActionResult<EventDto>> Create([FromBody] EventRequest request, CancellationToken cancellationToken)
    {
        EnsurePlatformWide(request);

        return await base.Create(request, cancellationToken);
    }

    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public override async Task<ActionResult<EventDto>> Update(int id, [FromBody] EventRequest request, CancellationToken cancellationToken)
    {
        EnsurePlatformWide(request);

        var existing = await service.GetByIdAsync(id, cancellationToken);
        EnsurePlatformWide(existing);

        return await base.Update(id, request, cancellationToken);
    }

    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public override async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var existing = await service.GetByIdAsync(id, cancellationToken);
        EnsurePlatformWide(existing);

        return await base.Delete(id, cancellationToken);
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
