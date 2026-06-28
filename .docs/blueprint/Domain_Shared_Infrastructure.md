# Bounded Context: Shared_Infrastructure

**Generated**: 2026-06-28T06:49:19.452411+00:00  
**Commit**: latest  
**Total Files**: 249

---

## 🤖 Agent Briefing (Read First)

This file contains the complete source for the **Shared_Infrastructure** bounded context.

**Your goals when reading this context:**
1. Build an accurate mental model of entities, behavior, and state transitions.
2. Identify cross-context interactions (see "Key Interactions" sections).
3. Note any architectural smells, duplicated logic, or unnecessary abstractions.
4. Track how this context communicates with others (especially via events).

**Focus areas for deep analysis:**
- Domain entities with rich behavior (not anemic)
- Service orchestration and access control
- State machines / workflow logic
- Cross-domain event contracts

---

## File: `eNote\eNote.API\Consumers\RentalStatusChangedPushConsumer.cs`
**Hash**: `49922ce325bf` | **Size**: 1060 chars

**Classes**: RentalStatusChangedPushConsumer
```cs
﻿using eNote.API.Hubs;
using eNote.Application.Features.Communication.Notifications;
using eNote.Contracts.Rentals;
using MassTransit;
using Microsoft.AspNetCore.SignalR;

namespace eNote.API.Consumers;

public sealed class RentalStatusChangedPushConsumer(IHubContext<NotificationHub> hubContext, ILogger<RentalStatusChangedPushConsumer> logger) : IConsumer<RentalStatusChanged>
{
    public async Task Consume(ConsumeContext<RentalStatusChanged> context)
    {
        var message = context.Message;

        var payload = new NotificationPushDto()
        {
            RentalId = message.RentalId,
            Title = message.Title,
            Body = message.Body,
            CreatedAt = message.OccurredAtUtc
        };

        await hubContext.Clients.Group(NotificationHub.UserGroup(message.StudentUserId)).SendAsync(NotificationHub.ReceiveMethod, payload, context.CancellationToken);

        logger.LogInformation("Pushed rental notification to SignalR group for user {UserId}, rental {RentalId}.", message.StudentUserId, message.RentalId);
    }
}

```

---

## File: `eNote\eNote.API\Controllers\Admin\AdminAddressController.cs`
**Hash**: `e436904fb474` | **Size**: 495 chars

**Classes**: AdminAddressController
```cs
﻿using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.ReferenceData.Addresses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/admin/addresses")]
public sealed class AdminAddressController(IAddressService service)
    : ReferenceCrudController<AddressReferenceDto, AddressRequest, AddressSearchObject>(service)
{
}

```

---

## File: `eNote\eNote.API\Controllers\Admin\AdminInstructorController.cs`
**Hash**: `1172236f4fc4` | **Size**: 1058 chars

**Classes**: AdminInstructorController
```cs
using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Instructors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/admin/instructors")]
public sealed class AdminInstructorController(IAdminInstructorService service) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InstructorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InstructorDto>>> GetPaged([FromQuery] InstructorSearchObject search)
    {
        PagedResult<InstructorDto> result = await service.GetPagedAsync(search);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InstructorDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstructorDto>> GetById(int id)
    {
        InstructorDto dto = await service.GetByIdAsync(id);
        return Ok(dto);
    }
}

```

---

## File: `eNote\eNote.API\Controllers\Admin\AdminInstrumentTypeController.cs`
**Hash**: `aa8e271642b2` | **Size**: 534 chars

**Classes**: AdminInstrumentTypeController
```cs
﻿using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/admin/instrument-types")]
public sealed class AdminInstrumentTypeController(IInstrumentTypeService service)
    : ReferenceCrudController<InstrumentTypeDto, InstrumentTypeRequest, InstrumentTypeSearchObject>(service)
{
}

```

---

## File: `eNote\eNote.API\Controllers\Admin\AdminMusicStoreController.cs`
**Hash**: `239fd416d399` | **Size**: 506 chars

**Classes**: AdminMusicStoreController
```cs
﻿using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.ReferenceData.MusicStores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/admin/music-stores")]
public sealed class AdminMusicStoreController(IMusicStoreService service)
    : ReferenceCrudController<MusicStoreDto, MusicStoreRequest, MusicStoreSearchObject>(service)
{
}

```

---

## File: `eNote\eNote.API\Controllers\Admin\AdminUsersController.cs`
**Hash**: `1d742f005401` | **Size**: 1910 chars

**Classes**: AdminUsersController
```cs
using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Admin;

[Authorize(Roles = AppRoles.Administrator)]
[Route("api/admin/users")]
public sealed class AdminUsersController(IUserProfileService profileService, IUserProvisioningService provisioningService) : CoreController
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> GetById(int id)
    {
        var profile = await profileService.GetUserAsync(id);

        if (profile is null)
        {
            return NotFound();
        }

        return Ok(profile);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Provision([FromBody] UserProvisionRequest request)
    {
        (var userId, var error) = await provisioningService.ProvisionUserAsync(request);

        if (error is not null)
        {
            return BadRequest(new
            {
                message = error
            });
        }

        return CreatedAtAction(nameof(GetById), new
        {
            id = userId
        }, new
        {
            userId
        });
    }

    [HttpPut("{id:int}/membership")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMembership(int id, [FromBody] UpdateMembershipRequest request)
    {
        await provisioningService.UpdateMembershipAsync(id, request);
        return NoContent();
    }
}

```

---

## File: `eNote\eNote.API\Controllers\Announcements\InstructorAnnouncementController.cs`
**Hash**: `0f733a10658c` | **Size**: 3204 chars

**Classes**: InstructorAnnouncementController
```cs
﻿using eNote.API.Controllers.Base;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Communication.Announcements;
using eNote.Application.Features.Communication.Announcements.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Announcements;

[Authorize(Roles = AppRoles.Instructor)]
[Route("api/instructor/courses/{courseId:int}/announcements")]
public sealed class InstructorAnnouncementController(ICourseAnnouncementService announcementService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AnnouncementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AnnouncementDto>>> GetForCourse(int courseId, [FromQuery] AnnouncementSearchObject search)
    {
        var result = await announcementService.GetForCourseAsync(courseId, search);
        return Ok(result);
    }

    [HttpGet("{announcementId:int}")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnnouncementDto>> GetById(int courseId, int announcementId)
    {
        var result = await announcementService.GetByIdForCourseAsync(courseId, announcementId);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AnnouncementDto>> Create(int courseId, [FromBody] AnnouncementRequest request)
    {
        var result = await announcementService.CreateForCourseAsync(courseId, request);
        return CreatedAtAction(nameof(GetById), new
        {
            courseId,
            announcementId = result.Id
        }, result);
    }

    [HttpPut("{announcementId:int}")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnnouncementDto>> Update(int courseId, int announcementId, [FromBody] AnnouncementRequest request)
    {
        var result = await announcementService.UpdateForCourseAsync(courseId, announcementId, request);
        return Ok(result);
    }

    [HttpDelete("{announcementId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int courseId, int announcementId)
    {
        await announcementService.DeleteForCourseAsync(courseId, announcementId);
        return NoContent();
    }

    [HttpPost("{announcementId:int}/image")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AnnouncementDto>> UploadImage(int courseId, int announcementId, IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = Messages.FileNotProvided });
        }

        await using Stream stream = file.OpenReadStream();

        var result = await announcementService.UploadImageForCourseAsync(courseId, announcementId, stream, file.FileName, file.ContentType, ct);

        return Ok(result);
    }
}

```

---

## File: `eNote\eNote.API\Controllers\Announcements\StoreAnnouncementController.cs`
**Hash**: `a25715634d3c` | **Size**: 2999 chars

**Classes**: StoreAnnouncementController
```cs
﻿using eNote.API.Controllers.Base;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Communication.Announcements;
using eNote.Application.Features.Communication.Announcements.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Announcements;

[Authorize(Roles = AppRoles.StoreEmployee)]
[Route("api/shop/announcements")]
public sealed class StoreAnnouncementController(IStoreAnnouncementService announcementService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AnnouncementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AnnouncementDto>>> GetForStore([FromQuery] AnnouncementSearchObject search)
    {
        var result = await announcementService.GetForStoreAsync(search);
        return Ok(result);
    }

    [HttpGet("{announcementId:int}")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnnouncementDto>> GetById(int announcementId)
    {
        var result = await announcementService.GetByIdForStoreAsync(announcementId);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<AnnouncementDto>> Create([FromBody] AnnouncementRequest request)
    {
        var result = await announcementService.CreateForStoreAsync(request);
        return CreatedAtAction(nameof(GetById), new
        {
            announcementId = result.Id
        }, result);
    }

    [HttpPut("{announcementId:int}")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnnouncementDto>> Update(int announcementId, [FromBody] AnnouncementRequest request)
    {
        var result = await announcementService.UpdateForStoreAsync(announcementId, request);
        return Ok(result);
    }

    [HttpDelete("{announcementId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int announcementId)
    {
        await announcementService.DeleteForStoreAsync(announcementId);
        return NoContent();
    }

    [HttpPost("{announcementId:int}/image")]
    [ProducesResponseType(typeof(AnnouncementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AnnouncementDto>> UploadImage(int announcementId, IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = Messages.FileNotProvided });
        }

        await using Stream stream = file.OpenReadStream();

        var result = await announcementService.UploadImageForStoreAsync(announcementId, stream, file.FileName, file.ContentType, ct);

        return Ok(result);
    }
}

```

---

## File: `eNote\eNote.API\Controllers\Announcements\StudentAnnouncementController.cs`
**Hash**: `28b62356602b` | **Size**: 901 chars

**Classes**: StudentAnnouncementController
```cs
﻿using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Communication.Announcements;
using eNote.Application.Features.Communication.Announcements.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Announcements;

[Authorize(Roles = AppRoles.Student)]
[Route("api/student/announcements")]
public sealed class StudentAnnouncementController(IStudentAnnouncementService announcementService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AnnouncementDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AnnouncementDto>>> GetFeed([FromQuery] AnnouncementSearchObject search)
    {
        var result = await announcementService.GetFeedForStudentAsync(search);
        return Ok(result);
    }
}

```

---

## File: `eNote\eNote.API\Controllers\Base\CoreController.cs`
**Hash**: `5954e0cd2422` | **Size**: 937 chars

**Classes**: CoreController
```cs
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace eNote.API.Controllers.Base;

[ApiController]
[Authorize]
public abstract class CoreController : ControllerBase
{
    protected string CurrentTokenJti => User.FindFirstValue(JwtRegisteredClaimNames.Jti) ?? throw new AuthenticationException(Messages.InvalidUserClaim);

    protected DateTime CurrentTokenExpiresAtUtc
    {
        get
        {
            var exp = User.FindFirstValue(JwtRegisteredClaimNames.Exp);

            if (exp is null || !long.TryParse(exp, out var unixSeconds))
            {
                throw new AuthenticationException(Messages.InvalidUserClaim);
            }

            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
        }
    }
}

```

---

## File: `eNote\eNote.API\Controllers\Base\ReferenceCrudController.cs`
**Hash**: `474bbcfadb13` | **Size**: 1847 chars

**Classes**: ReferenceCrudController
```cs
using eNote.Application.Common.Paging;
using eNote.Application.Common.Search;
using eNote.Application.Features.Rentals.ReferenceData;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Base;

public abstract class ReferenceCrudController<TDto, TRequest, TSearch>(IReferenceCrudService<TDto, TRequest, TSearch> service) : CoreController where TSearch : BaseSearchObject
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TDto>>> GetPaged([FromQuery] TSearch search)
    {
        PagedResult<TDto> result = await service.GetPagedAsync(search);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<TDto>> GetById(int id)
    {
        TDto dto = await service.GetByIdAsync(id);
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<TDto>> Create([FromBody] TRequest request)
    {
        TDto dto = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = GetDtoId(dto) }, dto);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<TDto>> Update(int id, [FromBody] TRequest request)
    {
        TDto dto = await service.UpdateAsync(id, request);
        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }

    private static object GetDtoId(TDto dto) => typeof(TDto).GetProperty("Id")?.GetValue(dto) ?? throw new InvalidOperationException($"{typeof(TDto).Name} must expose an Id property.");
}

```

---

## File: `eNote\eNote.API\Controllers\Files\UploadsController.cs`
**Hash**: `ab3158891745` | **Size**: 2992 chars

**Classes**: UploadsController
```cs
using eNote.API.Controllers.Base;
using eNote.Application.Common.Interfaces;
using eNote.Application.Features.Files.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Files;

[ApiController]
[Route("api/uploads")]
public sealed class UploadsController(IWebHostEnvironment env, IFileAccessService fileAccess, ICurrentUserService currentUser) : CoreController
{
    [AllowAnonymous]
    [HttpGet("instruments/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetInstrument(string fileName) => Serve("instruments", fileName);

    [Authorize]
    [HttpGet("announcements/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetAnnouncement(string fileName) => Serve("announcements", fileName);

    [Authorize]
    [HttpGet("assignments/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAssignment(string fileName, CancellationToken cancellationToken)
    {
        if (!IsSafeFileName(fileName))
        {
            return BadRequest();
        }

        if (!await fileAccess.CanAccessAssignmentFileAsync(currentUser.UserId, fileName, cancellationToken))
        {
            return Forbid();
        }

        return Serve("assignments", fileName);
    }

    private IActionResult Serve(string subfolder, string fileName)
    {
        if (!IsSafeFileName(fileName))
        {
            return BadRequest();
        }

        var uploadsRoot = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads", subfolder);
        var fullPath = Path.GetFullPath(Path.Combine(uploadsRoot, fileName));

        if (!fullPath.StartsWith(Path.GetFullPath(uploadsRoot), StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(fullPath))
        {
            return NotFound();
        }

        var contentType = GetContentType(fileName);

        return PhysicalFile(fullPath, contentType, enableRangeProcessing: true);
    }

    private static bool IsSafeFileName(string fileName) => !string.IsNullOrWhiteSpace(fileName) && fileName == Path.GetFileName(fileName) && !fileName.Contains("..", StringComparison.Ordinal);

    private static string GetContentType(string fileName) => Path.GetExtension(fileName).ToLowerInvariant()
        switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".pdf" => "application/pdf",
        _ => "application/octet-stream"
    };
}
```

---

## File: `eNote\eNote.API\Controllers\Instruments\PublicInstrumentController.cs`
**Hash**: `ed3a5a2d732d` | **Size**: 1059 chars

**Classes**: PublicInstrumentController
```cs
﻿using eNote.Application.Common.Paging;
using eNote.Application.Features.Rentals.Instruments;
using eNote.Application.Features.Rentals.Instruments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Instruments;

[ApiController]
[AllowAnonymous]
[Route("api/instruments/public")]
public sealed class PublicInstrumentController(IInstrumentService instrumentService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InstrumentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InstrumentDto>>> GetPaged([FromQuery] InstrumentSearchObject search)
    {
        var result = await instrumentService.GetPublicPagedAsync(search);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentDto>> GetById(int id)
    {
        var result = await instrumentService.GetPublicByIdAsync(id);
        return Ok(result);
    }
}

```

---

## File: `eNote\eNote.API\Controllers\Instruments\StoreInstrumentController.cs`
**Hash**: `855c2d842cf4` | **Size**: 2737 chars

**Classes**: StoreInstrumentController
```cs
using eNote.API.Controllers.Base;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.Instruments;
using eNote.Application.Features.Rentals.Instruments.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Instruments;

[Authorize(Roles = AppRoles.StoreEmployee)]
[Route("api/shop/instruments")]
public sealed class StoreInstrumentController(IInstrumentService instrumentService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<InstrumentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InstrumentDto>>> GetPaged([FromQuery] InstrumentSearchObject search)
    {
        var result = await instrumentService.GetPagedAsync(search);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentDto>> GetById(int id)
    {
        var result = await instrumentService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<InstrumentDto>> Create([FromBody] InstrumentCreateRequest request)
    {
        var result = await instrumentService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new
        {
            id = result.Id
        }, result);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InstrumentDto>> Update(int id, [FromBody] InstrumentUpdateRequest request)
    {
        var result = await instrumentService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpPost("{id:int}/image")]
    [ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InstrumentDto>> UploadImage(int id, IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = Messages.FileNotProvided });
        }

        await using Stream stream = file.OpenReadStream();
        var result = await instrumentService.UploadImageAsync(id, stream, file.FileName, file.ContentType, ct);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id)
    {
        await instrumentService.DeleteAsync(id);
        return NoContent();
    }
}

```

---

## File: `eNote\eNote.API\Controllers\Instruments\StudentInstrumentController.cs`
**Hash**: `15b8dcc8139b` | **Size**: 1141 chars

**Classes**: StudentInstrumentController
```cs
﻿using eNote.API.Controllers.Base;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.Recommendations;
using eNote.Application.Features.Rentals.Recommendations.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Instruments;

[Authorize(Roles = AppRoles.Student)]
[Route("api/student/instruments")]
public sealed class StudentInstrumentController(IRecommendationService recommendationService) : CoreController
{
    [HttpGet("recommended")]
    [ProducesResponseType(typeof(IReadOnlyList<InstrumentRecommendationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InstrumentRecommendationDto>>> GetRecommended([FromQuery] int count = 5)
    {
        var result = await recommendationService.GetRecommendedInstrumentsAsync(count);
        return Ok(result);
    }

    [HttpPost("{id:int}/view")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecordView(int id)
    {
        await recommendationService.RecordInstrumentViewAsync(id);
        return NoContent();
    }
}

```

---

## File: `eNote\eNote.API\Controllers\LectureNotes\InstructorLectureNoteController.cs`
**Hash**: `a7412e297f10` | **Size**: 2230 chars

**Classes**: InstructorLectureNoteController
```cs
﻿using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.LectureNotes;
using eNote.Application.Features.Academic.LectureNotes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.LectureNotes;

[Authorize(Roles = AppRoles.Instructor)]
[Route("api/instructor/lectures/{lectureId:int}/notes")]
public sealed class InstructorLectureNoteController(ILectureNoteService service) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LectureNoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LectureNoteDto>>> GetForLecture(int lectureId, [FromQuery] LectureNoteSearchObject search)
    {
        var result = await service.GetForLectureAsync(lectureId, search);
        return Ok(result);
    }

    [HttpGet("{noteId:int}")]
    [ProducesResponseType(typeof(LectureNoteDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LectureNoteDto>> GetById(int lectureId, int noteId)
    {
        var dto = await service.GetByIdForInstructorAsync(lectureId, noteId);
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(LectureNoteDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<LectureNoteDto>> Create(int lectureId, [FromBody] LectureNoteRequest request)
    {
        var dto = await service.CreateAsync(lectureId, request);
        return CreatedAtAction(nameof(GetById), new
        {
            lectureId,
            noteId = dto.Id
        }, dto);
    }

    [HttpPut("{noteId:int}")]
    [ProducesResponseType(typeof(LectureNoteDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LectureNoteDto>> Update(int lectureId, int noteId, [FromBody] LectureNoteRequest request)
    {
        var dto = await service.UpdateAsync(lectureId, noteId, request);
        return Ok(dto);
    }

    [HttpDelete("{noteId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int lectureId, int noteId)
    {
        await service.DeleteAsync(lectureId, noteId);
        return NoContent();
    }
}

```

---

## File: `eNote\eNote.API\Controllers\LectureNotes\StudentLectureNoteController.cs`
**Hash**: `ebe1f91e9d59` | **Size**: 1202 chars

**Classes**: StudentLectureNoteController
```cs
﻿using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.LectureNotes;
using eNote.Application.Features.Academic.LectureNotes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.LectureNotes;

[Authorize(Roles = AppRoles.Student)]
[Route("api/student/lectures/{lectureId:int}/notes")]
public sealed class StudentLectureNoteController(ILectureNoteService service) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LectureNoteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LectureNoteDto>>> GetForLecture(int lectureId, [FromQuery] LectureNoteSearchObject search)
    {
        var result = await service.GetForStudentAsync(lectureId, search);
        return Ok(result);
    }

    [HttpGet("{noteId:int}")]
    [ProducesResponseType(typeof(LectureNoteDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LectureNoteDto>> GetById(int lectureId, int noteId)
    {
        var dto = await service.GetByIdForStudentAsync(lectureId, noteId);
        return Ok(dto);
    }
}

```

---

## File: `eNote\eNote.API\Controllers\Lectures\InstructorLectureController.cs`
**Hash**: `bee65a0797ca` | **Size**: 3448 chars

**Classes**: InstructorLectureController
```cs
﻿using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Lectures;
using eNote.Application.Features.Academic.Lectures.Services;
using eNote.Application.Features.Reports.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Lectures;

[Authorize(Roles = AppRoles.Instructor)]
[Route("api/instructor/lectures")]
public sealed class InstructorLectureController(ILectureService service, ILectureAttendanceService attendanceService, IReportService reportService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LectureDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LectureDto>>> GetMyLectures([FromQuery] LectureSearchObject search)
    {
        var result = await service.GetPagedForInstructorAsync(search);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LectureDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LectureDto>> GetById(int id)
    {
        var dto = await service.GetByIdForInstructorAsync(id);
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(LectureDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<LectureDto>> Create([FromBody] LectureCreateRequest request)
    {
        var dto = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new
        {
            id = dto.Id
        }, dto);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(LectureDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LectureDto>> Update(int id, [FromBody] LectureUpdateRequest request)
    {
        var dto = await service.UpdateAsync(id, request);
        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(LectureDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LectureDto>> Cancel(int id)
    {
        var dto = await service.CancelAsync(id);
        return Ok(dto);
    }

    [HttpGet("{id:int}/attendance/report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttendanceReport(int id, CancellationToken cancellationToken)
    {
        var pdf = await reportService.GenerateLectureAttendancePdfAsync(id, cancellationToken);
        return File(pdf, "application/pdf", $"lecture-{id}-attendance.pdf");
    }

    [HttpGet("{id:int}/attendance")]
    [ProducesResponseType(typeof(PagedResult<AttendanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AttendanceDto>>> GetAttendance(int id, [FromQuery] AttendanceSearchObject search)
    {
        var result = await attendanceService.GetAttendanceAsync(id, search);
        return Ok(result);
    }

    [HttpPut("{id:int}/attendance")]
    [ProducesResponseType(typeof(AttendanceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AttendanceDto>> MarkAttendance(int id, [FromBody] MarkAttendanceRequest request)
    {
        var dto = await attendanceService.MarkAttendanceAsync(id, request);
        return Ok(dto);
    }
}

```

---

## File: `eNote\eNote.API\Controllers\Lectures\StudentLectureController.cs`
**Hash**: `1886f3415a8f` | **Size**: 1448 chars

**Classes**: StudentLectureController
```cs
﻿using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Lectures;
using eNote.Application.Features.Academic.Lectures.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Lectures;

[Authorize(Roles = AppRoles.Student)]
[Route("api/student/lectures")]
public sealed class StudentLectureController(
    ILectureService service,
    ILectureAttendanceService attendanceService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LectureDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LectureDto>>> GetAvailable([FromQuery] LectureSearchObject search)
    {
        var result = await service.GetPagedForStudentAsync(search);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LectureDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LectureDto>> GetById(int id)
    {
        var dto = await service.GetByIdForStudentAsync(id);
        return Ok(dto);
    }

    [HttpPost("{id:int}/rsvp")]
    [ProducesResponseType(typeof(RsvpResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RsvpResponse>> Rsvp(int id, [FromBody] RsvpRequest request)
    {
        var response = await attendanceService.RsvpAsync(id, request);
        return Ok(response);
    }
}

```

---

## File: `eNote\eNote.API\Controllers\Notifications\StudentNotificationController.cs`
**Hash**: `a60cf57005ab` | **Size**: 1782 chars

**Classes**: StudentNotificationController
```cs
﻿using eNote.API.Controllers.Base;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Communication.Notifications;
using eNote.Application.Features.Communication.Notifications.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Notifications;

[Authorize(Roles = AppRoles.Student)]
[Route("api/student/notifications")]
public sealed class StudentNotificationController(INotificationService notificationService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<NotificationDto>>> GetPaged([FromQuery] NotificationSearchObject search)
    {
        var result = await notificationService.GetPagedAsync(search);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(NotificationUnreadCountDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationUnreadCountDto>> GetUnreadCount()
    {
        var result = await notificationService.GetUnreadCountAsync();
        return Ok(result);
    }

    [HttpPatch("{id:int}/read")]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationDto>> MarkRead(int id)
    {
        var result = await notificationService.MarkReadAsync(id);
        return Ok(result);
    }

    [HttpPatch("read-all")]
    [ProducesResponseType(typeof(NotificationUnreadCountDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationUnreadCountDto>> MarkAllRead()
    {
        var result = await notificationService.MarkAllReadAsync();
        return Ok(result);
    }
}

```

---

## File: `eNote\eNote.API\Controllers\Users\UsersController.cs`
**Hash**: `6cb3a653205f` | **Size**: 3435 chars

**Classes**: UsersController
```cs
﻿using eNote.API.Controllers.Base;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Users;

[Route("api/users")]
public sealed class UsersController(
    IUserProfileService profileService,
    IUserSelfService selfService) : CoreController
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> GetCurrentUser()
    {
        var profile = await profileService.GetCurrentUserAsync();

        if (profile is null)
        {
            return NotFound();
        }

        return Ok(profile);
    }

    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        (var success, var error) = await selfService.UpdateProfileAsync(request);

        if (!success)
        {
            return BadRequest(new
            {
                message = error
            });
        }

        return NoContent();
    }

    [HttpPut("me/picture")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadPicture(IFormFile file)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded." });
        }

        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);

        (var success, var error) = await selfService.UpdatePictureAsync(buffer.ToArray());

        if (!success)
        {
            return BadRequest(new { message = error });
        }

        return NoContent();
    }

    [HttpGet("me/picture")]
    [Produces("image/jpeg", "image/png", "image/webp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPicture()
    {
        (var data, var contentType) = await selfService.GetPictureAsync();

        if (data is null || contentType is null)
        {
            return NotFound();
        }

        return File(data, contentType);
    }

    [HttpDelete("me/picture")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeletePicture()
    {
        (var success, var error) = await selfService.DeletePictureAsync();

        if (!success)
        {
            return BadRequest(new { message = error });
        }

        return NoContent();
    }

    [HttpPut("me/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        (var success, var error) = await selfService.ChangePasswordAsync(request);

        if (!success)
        {
            return BadRequest(new
            {
                message = error
            });
        }

        return NoContent();
    }
}

```

---

## File: `eNote\eNote.API\Extensions\ApplicationServiceExtensions.cs`
**Hash**: `f9084e1b29ab` | **Size**: 2471 chars

**Classes**: ApplicationServiceExtensions
### Key Cross-Cutting Interactions
- Uses **ICurrentActor** → Current actor resolution
- Uses **IUserProfileLookup** → User profile lookup
- Uses **RabbitMQ|IEventBus|Outbox** → Async integration events (RabbitMQ)
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.API.Services;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Communication.Announcements.Services;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;
using eNote.Infrastructure.Data;
using eNote.Infrastructure.Identity;
using eNote.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

namespace eNote.API.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ENoteContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
            sql => sql.MigrationsAssembly("eNote.Infrastructure")));

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        services.AddSingleton<IClock, SystemClock>();

        services.Scan(scan => scan
            .FromAssembliesOf(typeof(AuthService), typeof(CourseService))
            .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Service") && !type.IsAbstract))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.AddScoped<ICourseAnnouncementService, AnnouncementService>();
        services.AddScoped<IStoreAnnouncementService, AnnouncementService>();
        services.AddScoped<IStudentAnnouncementService, AnnouncementService>();
        services.AddScoped<IAdminInstructorService, AdminInstructorService>();

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICurrentActor, CurrentActor>();
        services.AddScoped<IUserProfileLookup, UserProfileLookup>();
        services.AddScoped<IRentalStateMachine, RentalStateMachine>();
        services.AddScoped<IRentalNotificationDispatcher, RentalNotificationDispatcher>();

        services.AddScoped<IAppDbContext>(x => x.GetRequiredService<ENoteContext>());
        services.AddHostedService<RentalNotificationOutboxPublisher>();

        return services;
    }
}

```

---

## File: `eNote\eNote.API\Extensions\ConfigurationExtensions.cs`
**Hash**: `8ad4540a58aa` | **Size**: 1404 chars

**Classes**: ConfigurationExtensions
### Key Cross-Cutting Interactions
- Uses **RabbitMQ|IEventBus|Outbox** → Async integration events (RabbitMQ)

```cs
using eNote.Infrastructure.Configuration;
using eNote.Infrastructure.Messaging;

namespace eNote.API.Extensions;

public static class ConfigurationExtensions
{
    public static void LoadDotEnv() => DotEnvConfiguration.Load();

    public static void ValidateRequiredSettings(this IConfiguration configuration)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")))
        {
            errors.Add("ConnectionStrings__DefaultConnection");
        }

        var jwtKey = configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            errors.Add("JWT__Key");
        }
        else if (jwtKey.Length < 32)
        {
            errors.Add("JWT__Key (minimum 32 characters)");
        }

        if (string.IsNullOrWhiteSpace(configuration["Jwt:Issuer"]))
        {
            errors.Add("JWT__Issuer");
        }

        if (string.IsNullOrWhiteSpace(configuration["Jwt:Audience"]))
        {
            errors.Add("JWT__Audience");
        }

        if (!RabbitMqConfiguration.IsConfigured(configuration))
        {
            errors.Add("RabbitMQ__Host (or RabbitMQ__User)");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException("Missing or invalid required configuration values: " + string.Join(", ", errors));
        }
    }
}

```

---

## File: `eNote\eNote.API\Extensions\CorsExtensions.cs`
**Hash**: `850e2f3a38c9` | **Size**: 1194 chars

**Classes**: CorsExtensions
```cs
namespace eNote.API.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "ENoteCors";

    public static IServiceCollection AddApplicationCors(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? configuration["Cors:AllowedOrigins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                if (origins.Length == 0)
                {
                    if (environment.IsDevelopment())
                    {
                        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                        return;
                    }

                    throw new InvalidOperationException("Cors:AllowedOrigins must be configured for non-development environments.");
                }

                policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            });
        });

        return services;
    }
}

```

---

## File: `eNote\eNote.API\Extensions\HealthCheckExtensions.cs`
**Hash**: `9412171086f5` | **Size**: 387 chars

**Classes**: HealthCheckExtensions
### Key Cross-Cutting Interactions
- Uses **RabbitMQ|IEventBus|Outbox** → Async integration events (RabbitMQ)

```cs
using eNote.API.Health;

namespace eNote.API.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddApplicationHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("sqlserver")
            .AddCheck<RabbitMqHealthCheck>("rabbitmq");

        return services;
    }
}

```

---

## File: `eNote\eNote.API\Extensions\IdentityExtensions.cs`
**Hash**: `6960d3d77577` | **Size**: 3536 chars

**Classes**: IdentityExtensions
```cs
﻿using eNote.Application.Common.Localization;
using eNote.Application.Features.Identity.Auth.Services;
using eNote.Infrastructure.Data;
using eNote.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace eNote.API.Extensions;

public static class IdentityExtensions
{
    public static IServiceCollection AddApplicationIdentity(this IServiceCollection services)
    {
        services.AddIdentityCore<AppUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequiredLength = 8;
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddRoles<AppRole>()
        .AddEntityFrameworkStores<ENoteContext>()
        .AddSignInManager<SignInManager<AppUser>>()
        .AddDefaultTokenProviders();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var key = Encoding.UTF8.GetBytes(config["Jwt:Key"]!);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var jti = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);

                        if (string.IsNullOrWhiteSpace(jti))
                        {
                            return;
                        }

                        var revocation = context.HttpContext.RequestServices.GetRequiredService<ITokenRevocationService>();

                        if (await revocation.IsRevokedAsync(jti, context.HttpContext.RequestAborted))
                        {
                            context.Fail(Messages.TokenRevoked);
                        }
                    }
                };
            });

        return services;
    }
}

```

---

## File: `eNote\eNote.API\Extensions\LoggingExtensions.cs`
**Hash**: `92d731578fa0` | **Size**: 667 chars

**Classes**: LoggingExtensions
```cs
using Serilog;

namespace eNote.API.Extensions;

public static class LoggingExtensions
{
    public static IHostBuilder UseApplicationLogging(this IHostBuilder host) =>
        host.UseSerilog((ctx, services, cfg) => cfg
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                path: "logs/enote-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));
}

```

---

## File: `eNote\eNote.API\Extensions\MapsterExtensions.cs`
**Hash**: `dcb2cea19e6b` | **Size**: 490 chars

**Classes**: MapsterExtensions
```cs
using eNote.Application.Features.Communication.Announcements;
using Mapster;

namespace eNote.API.Extensions;

public static class MapsterExtensions
{
    public static IServiceCollection AddMapsterMappings(this IServiceCollection services)
    {
        var config = new TypeAdapterConfig();

        config.Scan(typeof(AnnouncementMappingConfig).Assembly);
        config.Compile();

        services.AddSingleton(config);
        services.AddMapster();

        return services;
    }
}

```

---

## File: `eNote\eNote.API\Extensions\MessagingExtensions.cs`
**Hash**: `1c5fb26f73a4` | **Size**: 386 chars

**Classes**: MessagingExtensions
### Key Cross-Cutting Interactions
- Uses **RabbitMQ|IEventBus|Outbox** → Async integration events (RabbitMQ)

```cs
using eNote.API.Consumers;
using eNote.Infrastructure.Messaging;

namespace eNote.API.Extensions;

public static class MessagingExtensions
{
    public static IServiceCollection AddApplicationMessaging(this IServiceCollection services, IConfiguration configuration) =>
        services.AddRabbitMqMassTransit(configuration, bus => bus.AddConsumer<RentalStatusChangedPushConsumer>());
}

```

---

## File: `eNote\eNote.API\Extensions\MiddlewareExtensions.cs`
**Hash**: `b54120eba50e` | **Size**: 1776 chars

**Classes**: MiddlewareExtensions
```cs
﻿using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json;

namespace eNote.API.Extensions;

public static class MiddlewareExtensions
{
    public static WebApplication UseErrorHandling(this WebApplication app)
    {
        _ = app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                context.Response.ContentType = "application/json";

                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

                (var statusCode, var errorCode, var message) = exception switch
                {
                    AppException appEx => (appEx.StatusCode, appEx.ErrorCode, appEx.Message),
                    ArgumentException => (400, "error.bad_request", exception?.Message ?? Messages.BadRequest),
                    _ => (500, "error.internal", Messages.InternalError)
                };

                context.Response.StatusCode = statusCode;

                var logger = context.RequestServices.GetService<ILogger<WebApplication>>();

                logger?.LogError(exception, "Unhandled exception caught by middleware");

                var response = new ErrorResponse
                {
                    Status = statusCode,
                    Code = errorCode,
                    Message = message
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            });
        });

        return app;
    }

    private record ErrorResponse
    {
        public int Status { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }
}

```

---

## File: `eNote\eNote.API\Extensions\OpenAPIExtensions.cs`
**Hash**: `7985be6f24a1` | **Size**: 2327 chars

**Classes**: AnonymousOperationTransformer, BearerSecurityTransformer, OpenAPIExtensions
```cs
﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace eNote.API.Extensions;

public static class OpenAPIExtensions
{
    public static WebApplication MapScalarDocumentation(this WebApplication app)
    {
        app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
        app.MapOpenApi();
        app.MapScalarApiReference();

        return app;
    }

    public static IServiceCollection AddScalarDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecurityTransformer>();
            options.AddOperationTransformer<AnonymousOperationTransformer>();
        });

        return services;
    }
}

public sealed class BearerSecurityTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes.Add("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Unesite važeći JSON Web Token (JWT)."
        });

        document.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            }
        ];

        return Task.CompletedTask;
    }
}

public sealed class AnonymousOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;

        if (metadata.Any(m => m is IAllowAnonymous))
        {
            operation.Security = [];

            return Task.CompletedTask;
        }

        if (!metadata.Any(m => m is IAuthorizeData))
        {
            operation.Security = [];
        }

        return Task.CompletedTask;
    }
}

```

---

## File: `eNote\eNote.API\Extensions\RateLimitingExtensions.cs`
**Hash**: `9bbe6a507e46` | **Size**: 783 chars

**Classes**: RateLimitingExtensions
```cs
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace eNote.API.Extensions;

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";

    public static IServiceCollection AddApplicationRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter(AuthPolicy, opt =>
            {
                opt.PermitLimit = 10;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }
}

```

---

## File: `eNote\eNote.API\Extensions\SeedExtensions.cs`
**Hash**: `918c2769667a` | **Size**: 896 chars

**Classes**: SeedExtensions
```cs
﻿using eNote.Infrastructure.Data;
using eNote.Infrastructure.Data.Seed;
using Microsoft.EntityFrameworkCore;

namespace eNote.API.Extensions;

public static class SeedExtensions
{
    public static async Task<WebApplication> MigrateAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ENoteContext>();

        await context.Database.MigrateAsync();

        return app;
    }

    public static async Task<WebApplication> SeedDevelopmentData(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();

        var services = scope.ServiceProvider;
        await IdentitySeed.SeedAsync(services);

        var context = services.GetRequiredService<ENoteContext>();
        await DevelopmentDataSeed.SeedAsync(context);

        return app;
    }
}

```

---

## File: `eNote\eNote.API\Extensions\SignalRExtensions.cs`
**Hash**: `82642058c8d3` | **Size**: 236 chars

**Classes**: SignalRExtensions
```cs
namespace eNote.API.Extensions;

public static class SignalRExtensions
{
    public static IServiceCollection AddApplicationSignalR(this IServiceCollection services)
    {
        services.AddSignalR();
        return services;
    }
}

```

---

## File: `eNote\eNote.API\Extensions\ValidationExtensions.cs`
**Hash**: `4582c3540e17` | **Size**: 471 chars

**Classes**: ValidationExtensions
```cs
using eNote.Application.Features.Communication.Announcements;
using FluentValidation;
using FluentValidation.AspNetCore;

namespace eNote.API.Extensions;

public static class ValidationExtensions
{
    public static IServiceCollection AddApplicationValidation(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<AnnouncementMappingConfig>();

        return services;
    }
}

```

---

## File: `eNote\eNote.API\Health\DatabaseHealthCheck.cs`
**Hash**: `ab5629df40ce` | **Size**: 752 chars

**Classes**: DatabaseHealthCheck
### Key Cross-Cutting Interactions
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Infrastructure.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace eNote.API.Health;

public sealed class DatabaseHealthCheck(ENoteContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

            return canConnect ? HealthCheckResult.Healthy("SQL Server is reachable.") : HealthCheckResult.Unhealthy("SQL Server is not reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQL Server health check failed.", ex);
        }
    }
}

```

---

## File: `eNote\eNote.API\Health\RabbitMqHealthCheck.cs`
**Hash**: `2756e2ca08d9` | **Size**: 1195 chars

**Classes**: RabbitMqHealthCheck
### Key Cross-Cutting Interactions
- Uses **RabbitMQ|IEventBus|Outbox** → Async integration events (RabbitMQ)

```cs
using eNote.Infrastructure.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace eNote.API.Health;

public sealed class RabbitMqHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = RabbitMqConfiguration.GetHost(configuration),
                VirtualHost = RabbitMqConfiguration.GetVirtualHost(configuration),
                UserName = RabbitMqConfiguration.GetUsername(configuration),
                Password = RabbitMqConfiguration.GetPassword(configuration)
            };

            await using var connection = await factory.CreateConnectionAsync(cancellationToken);

            return connection.IsOpen ? HealthCheckResult.Healthy("RabbitMQ is reachable.") : HealthCheckResult.Unhealthy("RabbitMQ connection could not be opened.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ health check failed.", ex);
        }
    }
}

```

---

## File: `eNote\eNote.API\Hubs\NotificationHub.cs`
**Hash**: `dc526637823d` | **Size**: 741 chars

**Classes**: NotificationHub
```cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.IdentityModel.Tokens.Jwt;

namespace eNote.API.Hubs;

[Authorize]
public sealed class NotificationHub : Hub
{
    public const string HubPath = "/hubs/notifications";
    public const string ReceiveMethod = "ReceiveNotification";

    public static string UserGroup(int userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        var userIdValue = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (int.TryParse(userIdValue, out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        }

        await base.OnConnectedAsync();
    }
}

```

---

## File: `eNote\eNote.API\Program.cs`
**Hash**: `07da5e859bb0` | **Size**: 1701 chars

```cs
using eNote.API.Extensions;
using eNote.API.Hubs;
using Serilog;
using System.Text.Json.Serialization;

eNote.API.Extensions.ConfigurationExtensions.LoadDotEnv();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseApplicationLogging();
builder.Configuration.ValidateRequiredSettings();

builder.Services
    .AddApplicationDatabase(builder.Configuration)
    .AddApplicationIdentity()
    .AddJwtAuthentication(builder.Configuration)
    .AddAuthorization()
    .AddApplicationServices()
    .AddApplicationMessaging(builder.Configuration)
    .AddApplicationCors(builder.Configuration, builder.Environment)
    .AddApplicationRateLimiting()
    .AddResponseCompression(opts => opts.EnableForHttps = true)
    .AddMapsterMappings()
    .AddApplicationValidation()
    .AddApplicationSignalR()
    .AddScalarDocumentation();

builder.Services
    .AddControllers()
    .AddJsonOptions(x => x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddApplicationHealthChecks();

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseResponseCompression();
}
app.UseCors(CorsExtensions.PolicyName);
app.UseErrorHandling();

if (app.Environment.IsDevelopment())
{
    await app.MigrateAsync();
    app.MapScalarDocumentation();
    await app.SeedDevelopmentData();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();
app.MapHub<NotificationHub>(NotificationHub.HubPath);

app.Run();

```

---

## File: `eNote\eNote.API\Services\CurrentUserService.cs`
**Hash**: `1cf7d17829df` | **Size**: 882 chars

**Classes**: CurrentUserService
```cs
﻿using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace eNote.API.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public int UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;

            var id = user?.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(id, out var userId))
            {
                throw new AuthenticationException(Messages.InvalidUserClaim);
            }

            return userId;
        }
    }

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}

```

---

## File: `eNote\eNote.API\appsettings.Development.json`
**Hash**: `4f1dce8a3117` | **Size**: 363 chars

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:5059",
      "https://localhost:7239"
    ]
  },
  "Seed": {
    "DefaultPassword": "Test1234!"
  }
}

```

---

## File: `eNote\eNote.API\appsettings.json`
**Hash**: `7b7876ddd8cf` | **Size**: 355 chars

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Jwt": {
    "Key": "",
    "Issuer": "ENote.Api",
    "Audience": "ENote.Client",
    "ExpirationDays": 1
  },
  "Cors": {
    "AllowedOrigins": []
  }
}
```

---

## File: `eNote\eNote.API\libman.json`
**Hash**: `80df475c5002` | **Size**: 71 chars

```json
{
  "version": "3.0",
  "defaultProvider": "cdnjs",
  "libraries": []
}
```

---

## File: `eNote\eNote.Application\Common\Exceptions\AppException.cs`
**Hash**: `ee1111f502bc` | **Size**: 656 chars

**Classes**: AppException
```cs
namespace eNote.Application.Common.Exceptions;

public abstract class AppException(int statusCode, string errorCode, string? message = null) : Exception(message ?? GetDefaultMessage(statusCode))
{
    public int StatusCode { get; } = statusCode;
    public string ErrorCode { get; } = errorCode;

    private static string GetDefaultMessage(int statusCode) => statusCode switch
    {
        400 => Localization.Messages.BadRequest,
        401 => "Niste autorizovani.",
        403 => "Nemate pristup ovom resursu.",
        404 => Localization.Messages.NotFound,
        409 => "Sukob resursa.",
        _ => Localization.Messages.InternalError
    };
}

```

---

## File: `eNote\eNote.Application\Common\Exceptions\BusinessException.cs`
**Hash**: `2d52324d6cd9` | **Size**: 154 chars

**Classes**: BusinessException
```cs
namespace eNote.Application.Common.Exceptions;

public class BusinessException(string? message = null) : AppException(400, "error.business", message)
{
}

```

---

## File: `eNote\eNote.Application\Common\Exceptions\ConflictException.cs`
**Hash**: `381931baea33` | **Size**: 154 chars

**Classes**: ConflictException
```cs
namespace eNote.Application.Common.Exceptions;

public class ConflictException(string? message = null) : AppException(409, "error.conflict", message)
{
}

```

---

## File: `eNote\eNote.Application\Common\Exceptions\NotFoundException.cs`
**Hash**: `f1a1cb0bf214` | **Size**: 155 chars

**Classes**: NotFoundException
```cs
namespace eNote.Application.Common.Exceptions;

public class NotFoundException(string? message = null) : AppException(404, "error.not_found", message)
{
}

```

---

## File: `eNote\eNote.Application\Common\Interfaces\ICurrentActor.cs`
**Hash**: `72bab6703aaa` | **Size**: 367 chars

**Classes**: 
**Interfaces**: ICurrentActor
### Key Cross-Cutting Interactions
- Uses **ICurrentActor** → Current actor resolution

```cs
using eNote.Domain.Entities;

namespace eNote.Application.Common.Interfaces;

public interface ICurrentActor : ICurrentUserService
{
    Task<Student> GetCurrentStudentAsync();
    Task<int> GetCurrentStudentIdAsync();
    Task<Instructor> GetCurrentInstructorAsync();
    Task<MusicStoreEmployee> GetCurrentEmployeeAsync();
    Task<int> GetCurrentStoreIdAsync();
}

```

---

## File: `eNote\eNote.Application\Common\Interfaces\ICurrentUserService.cs`
**Hash**: `6a2e58c205e4` | **Size**: 147 chars

**Classes**: 
**Interfaces**: ICurrentUserService
```cs
namespace eNote.Application.Common.Interfaces;

public interface ICurrentUserService
{
    int UserId { get; }
    bool IsAuthenticated { get; }
}

```

---

## File: `eNote\eNote.Application\Common\Interfaces\IEmailService.cs`
**Hash**: `cf33b0025add` | **Size**: 191 chars

**Classes**: 
**Interfaces**: IEmailService
```cs
namespace eNote.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken = default);
}

```

---

## File: `eNote\eNote.Application\Common\Interfaces\IFileStorageService.cs`
**Hash**: `9b032e61d09c` | **Size**: 341 chars

**Classes**: 
**Interfaces**: IFileStorageService
```cs
namespace eNote.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream stream, string fileName, string contentType, string subfolder, CancellationToken ct = default);
    Task<string> SaveAssignmentAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default);
}

```

---

## File: `eNote\eNote.Application\Common\Interfaces\IRentalNotificationDispatcher.cs`
**Hash**: `a4a539e0a06f` | **Size**: 508 chars

**Classes**: 
**Interfaces**: IRentalNotificationDispatcher
```cs
﻿using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;

namespace eNote.Application.Common.Interfaces;

public interface IRentalNotificationDispatcher
{
    Task DispatchCreatedAsync(InstrumentRentalDto rental, int studentUserId, CancellationToken cancellationToken = default);
    Task DispatchTransitionAsync(InstrumentRentalDto rental, RentalTrigger trigger, int actorUserId, CancellationToken cancellationToken = default);
}

```

---

## File: `eNote\eNote.Application\Common\Localization\Messages.cs`
**Hash**: `b8781b9a9265` | **Size**: 6428 chars

**Classes**: Messages
```cs
namespace eNote.Application.Common.Localization;

public static class Messages
{
    public const string NotFound = "ID nije pronađen.";
    public const string BadRequest = "Neispravan zahtjev.";
    public const string InternalError = "Došlo je do greške na serveru.";

    public const string InvalidCredentials = "Pogrešno korisničko ime ili lozinka.";
    public const string AccountLocked = "Nalog je privremeno zaključan. Pokušajte ponovo kasnije.";
    public const string UsernameTaken = "Korisničko ime je već zauzeto.";
    public const string EmailTaken = "Email adresa je već registrovana.";
    public const string TokenRevoked = "Token je opozvan.";
    public const string InvalidUserClaim = "Autentificirani korisnik nema važeći identifikator.";
    public const string RoleMisconfigured = "Korisnički račun nije ispravno konfigurisan (uloge). Kontaktirajte administratora.";

    public const string UserSingleRoleRequired = "Korisnik mora imati tačno jednu ulogu.";
    public const string UnknownRole = "Nepoznata uloga.";
    public const string StoreNotFound = "Radnja nije pronađena.";

    public const string StudentProfileNotFound = "Student profil nije pronađen.";
    public const string InstructorProfileNotFound = "Instruktor profil nije pronađen.";
    public const string EmployeeProfileNotFound = "Profil uposlenika radnje nije pronađen.";
    public const string ActiveEmployeeStoreNotFound = "Profil uposlenika radnje nije pronađen ili nije aktivan.";

    public const string CourseNotFound = "Kurs nije pronađen.";
    public const string CourseIdRequired = "Kurs je obavezan.";
    public const string CourseNotOwned = "Niste vlasnik navedenog kursa.";

    public const string LectureNotFound = "Predavanje nije pronađeno.";
    public const string LectureCancelled = "Predavanje je otkazano.";
    public const string LectureFull = "Predavanje je popunjeno.";
    public const string LectureRsvpConflict = "Sukob pri rezervaciji predavanja. Pokušajte ponovo.";

    public const string InstrumentTypeNotFound = "Vrsta instrumenta ne postoji.";
    public const string InstrumentNotFound = "Instrument nije pronađen.";
    public const string InstrumentInactive = "Instrument nije aktivan.";
    public const string InstrumentReservedOrRented = "Instrument je rezervisan ili već iznajmljen.";
    public const string InstrumentDeleteBlocked = "Instrument se ne može obrisati jer je trenutno rezervisan ili iznajmljen.";

    public const string RentalNotFound = "Zahtjev nije pronađen.";
    public const string RentalNotFoundAfterUpdate = "Zahtjev nije pronađen nakon ažuriranja.";
    public const string RentalPendingRequired = "Već imate zahtjev na čekanju za ovaj instrument.";
    public const string RentalApprovePendingOnly = "Samo zahtjev na čekanju može biti odobren.";
    public const string RentalRejectPendingOnly = "Samo zahtjev na čekanju se može odbiti.";
    public const string RentalPickupApprovedOnly = "Samo odobreno iznajmljivanje se može preuzeti.";
    public const string RentalAlreadyPickedUp = "Instrument je već preuzet.";
    public const string RentalCompleteActiveOnly = "Samo aktivno iznajmljivanje se može završiti.";
    public const string RentalAlreadyCompleted = "Iznajmljivanje je već završeno.";
    public const string RentalCancelPendingOrApprovedOnly = "Samo zahtjev na čekanju ili odobren zahtjev se može otkazati.";
    public const string RentalCancelBlockedAfterPickup = "Instrument je već preuzet, otkazivanje nije moguće.";
    public const string RentalEarlyReturnActiveOnly = "Samo aktivno iznajmljivanje se može prijevremeno završiti.";
    public const string RentalNotPickedUp = "Instrument nije preuzet.";
    public const string RentalInstrumentMissing = "Instrument nije pronađen za ovaj zahtjev.";
    public const string RentalAccessDenied = "Nemate pravo nad ovim zahtjevom.";

    public const string NotificationNotFound = "Notifikacija nije pronađena.";

    public const string AnnouncementNotFound = "Obavijest nije pronađena.";
    public const string AnnouncementCourseForbidden = "Nemate pravo objavljivati obavijesti za ovaj kurs.";

    public const string AssignmentNotFound = "Zadatak nije pronađen.";
    public const string AssignmentAlreadySubmitted = "Zadatak je već predan.";
    public const string AssignmentSubmissionNotFound = "Predaja zadatka nije pronađena.";
    public const string AssignmentInvalidGrade = "Ocjena mora biti između 0 i 100.";
    public const string LectureNoteNotFound = "Bilješka predavanja nije pronađena.";
    public const string StudentNotEnrolled = "Student nije upisan na kurs.";
    public const string MembershipInactive = "Članarina nije aktivna. Kontaktirajte administratora.";
    public const string AssignmentPastDue = "Rok za predaju zadatka je istekao.";

    public const string AddressNotFound = "Adresa nije pronađena.";
    public const string AddressDeleteBlocked = "Adresa se ne može obrisati jer je povezana s korisnikom.";
    public const string InstrumentTypeDeleteBlocked = "Vrsta instrumenta se ne može obrisati jer je u upotrebi.";
    public const string MusicStoreDeleteBlocked = "Radnja se ne može obrisati jer sadrži instrumente ili zaposlenike.";

    public const string PasswordResetEmailSent = "Ako nalog postoji, poslat je token za reset lozinke.";
    public const string PasswordResetFailed = "Reset lozinke nije uspio. Provjerite token i pokušajte ponovo.";

    public const string FileNotProvided = "Fajl nije priložen.";
    public const string FileTooLarge = "Veličina fajla prelazi maksimalno dozvoljenih 5 MB.";
    public const string InvalidFileFormat = "Dozvoljeni formati su JPEG, PNG i WebP.";

    public static string RoleCreateFailed(string role, string errors) =>
        $"Greška pri kreiranju uloge {role}: {errors}";

    public static string UserCreateFailed(string username, string errors) =>
        $"Greška pri kreiranju korisnika {username}: {errors}";

    public static string UserUpdateFailed(string username, string errors) =>
        $"Greška pri ažuriranju korisnika {username}: {errors}";

    public static string UserRoleRemoveFailed(string username, string errors) =>
        $"Greška pri uklanjanju uloga korisnika {username}: {errors}";

    public static string UserRoleAssignFailed(string role, string username, string errors) =>
        $"Greška pri dodjeli uloge {role} korisniku {username}: {errors}";
}

```

---

## File: `eNote\eNote.Application\Common\Paging\PagedResult.cs`
**Hash**: `e9e81a39e6f9` | **Size**: 410 chars

**Classes**: PagedResult
```cs
namespace eNote.Application.Common.Paging;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int? TotalCount { get; init; }

    public int Count => Items.Count;
    public int? TotalPages => TotalCount.HasValue && PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : null;
}

```

---

## File: `eNote\eNote.Application\Common\Paging\PagingExtensions.cs`
**Hash**: `e8e4e6895afb` | **Size**: 4507 chars

**Classes**: PagingExtensions
```cs
﻿using eNote.Application.Common.Search;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Common.Paging;

public static class PagingExtensions
{
    public static async Task<PagedResult<TModel>> ToPagedResultAsync<TEntity, TModel>(this IQueryable<TEntity> query, int page, int pageSize, bool includeTotalCount, Func<TEntity, TModel> map, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, CancellationToken ct = default)
    {
        int? total = null;

        if (includeTotalCount)
        {
            total = await query.CountAsync(ct);
        }

        (page, pageSize) = PagingLimits.Normalize(page, pageSize);

        if (orderBy is not null)
        {
            query = orderBy(query);
        }

        var entities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<TModel>
        {
            Items = [.. entities.Select(map)],
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public static async Task<PagedResult<TModel>> ToPagedResultAsync<TEntity, TModel>
        (this IQueryable<TEntity> query, int page, int pageSize, bool includeTotalCount, Func<TEntity, Task<TModel>> mapAsync, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, CancellationToken ct = default)
    {
        int? total = null;

        if (includeTotalCount)
        {
            total = await query.CountAsync(ct);
        }

        (page, pageSize) = PagingLimits.Normalize(page, pageSize);

        if (orderBy is not null)
        {
            query = orderBy(query);
        }

        var entities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = await Task.WhenAll(entities.Select(mapAsync));

        return new PagedResult<TModel>
        {
            Items = [.. items],
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public static async Task<PagedResult<TModel>> ToPagedResultAsync<TEntity, TCtx, TModel>
        (this IQueryable<TEntity> query, int page, int pageSize, bool includeTotalCount, Func<IReadOnlyList<TEntity>, Task<TCtx>> loadContext, Func<TEntity, TCtx, TModel> map, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, CancellationToken ct = default)
    {
        int? total = null;

        if (includeTotalCount)
        {
            total = await query.CountAsync(ct);
        }

        (page, pageSize) = PagingLimits.Normalize(page, pageSize);

        if (orderBy is not null)
        {
            query = orderBy(query);
        }

        var entities = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var ctx = await loadContext(entities);

        return new PagedResult<TModel>
        {
            Items = [.. entities.Select(e => map(e, ctx))],
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public static Task<PagedResult<TModel>> ToPagedResultAsync<TEntity, TSearch, TModel>
        (this IQueryable<TEntity> query, TSearch search, Func<TEntity, TModel> map, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, CancellationToken ct = default)
        where TSearch : BaseSearchObject => query.ToPagedResultAsync(search.Page, search.PageSize, search.IncludeTotalCount, map, orderBy, ct);

    public static Task<PagedResult<TModel>> ToPagedResultAsync<TEntity, TSearch, TModel>
        (this IQueryable<TEntity> query, TSearch search, Func<TEntity, Task<TModel>> mapAsync, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, CancellationToken ct = default)
        where TSearch : BaseSearchObject => query.ToPagedResultAsync(search.Page, search.PageSize, search.IncludeTotalCount, mapAsync, orderBy, ct);

    public static Task<PagedResult<TModel>> ToPagedResultAsync<TEntity, TSearch, TCtx, TModel>
        (this IQueryable<TEntity> query, TSearch search, Func<IReadOnlyList<TEntity>, Task<TCtx>> loadContext, Func<TEntity, TCtx, TModel> map, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, CancellationToken ct = default)
        where TSearch : BaseSearchObject => query.ToPagedResultAsync(search.Page, search.PageSize, search.IncludeTotalCount, loadContext, map, orderBy, ct);
}

```

---

## File: `eNote\eNote.Application\Common\Paging\PagingLimits.cs`
**Hash**: `a9b02e3629d1` | **Size**: 455 chars

**Classes**: PagingLimits
```cs
namespace eNote.Application.Common.Paging;

public static class PagingLimits
{
    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 100;

    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? DefaultPageSize : pageSize;
        pageSize = pageSize > MaxPageSize ? MaxPageSize : pageSize;

        return (page, pageSize);
    }
}

```

---

## File: `eNote\eNote.Application\Common\Persistence\IAppDbContext.cs`
**Hash**: `160a2a9706f7` | **Size**: 405 chars

**Classes**: 
**Interfaces**: IAppDbContext
### Key Cross-Cutting Interactions
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace eNote.Application.Common.Persistence;

public interface IAppDbContext
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

```

---

## File: `eNote\eNote.Application\Common\Search\BaseSearchObject.cs`
**Hash**: `e48da0b09bd1` | **Size**: 217 chars

**Classes**: BaseSearchObject
```cs
namespace eNote.Application.Common.Search;

public class BaseSearchObject
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool IncludeTotalCount { get; set; } = true;
}

```

---

## File: `eNote\eNote.Application\Common\Search\QueryableFilterExtensions.cs`
**Hash**: `554f6f36aa57` | **Size**: 702 chars

**Classes**: QueryableFilterExtensions
```cs
using System.Linq.Expressions;

namespace eNote.Application.Common.Search;

public static class QueryableFilterExtensions
{
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, bool>> predicate) => condition ? query.Where(predicate) : query;
    public static IQueryable<T> WhereContainsIf<T>(this IQueryable<T> query, string? value, Expression<Func<T, bool>> predicate) => string.IsNullOrWhiteSpace(value) ? query : query.Where(predicate);
    public static IQueryable<T> WhereEqualsIf<T, TValue>(this IQueryable<T> query, TValue? value, Expression<Func<T, bool>> predicate) where TValue : struct => value.HasValue ? query.Where(predicate) : query;
}
```

---

## File: `eNote\eNote.Application\Common\Time\IClock.cs`
**Hash**: `a2742f3ac396` | **Size**: 99 chars

**Classes**: 
**Interfaces**: IClock
```cs
namespace eNote.Application.Common.Time;

public interface IClock
{
    DateTime UtcNow { get; }
}

```

---

## File: `eNote\eNote.Application\Common\Time\SystemClock.cs`
**Hash**: `d5045c0bfbf5` | **Size**: 134 chars

**Classes**: SystemClock
```cs
namespace eNote.Application.Common.Time;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

```

---

## File: `eNote\eNote.Application\Constants\AppRoles.cs`
**Hash**: `905781bde49a` | **Size**: 283 chars

**Classes**: AppRoles
```cs
namespace eNote.Application.Constants;

public static class AppRoles
{
    public const string Administrator = "Administrator";
    public const string Instructor = "Instructor";
    public const string Student = "Student";
    public const string StoreEmployee = "StoreEmployee";
}

```

---

## File: `eNote\eNote.Application\Features\Academic\LectureNotes\LectureNoteDto.cs`
**Hash**: `9e8ef5036c89` | **Size**: 261 chars

**Classes**: LectureNoteDto
```cs
namespace eNote.Application.Features.Academic.LectureNotes;

public class LectureNoteDto
{
    public int Id { get; set; }
    public int LectureId { get; set; }

    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
}

```

---

## File: `eNote\eNote.Application\Features\Academic\LectureNotes\LectureNoteRequest.cs`
**Hash**: `adfd6a856c44` | **Size**: 270 chars

**Classes**: LectureNoteRequest
```cs
using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Academic.LectureNotes;

public class LectureNoteRequest
{
    [Required]
    public string Title { get; set; } = null!;

    [Required]
    public string Content { get; set; } = null!;
}

```

---

## File: `eNote\eNote.Application\Features\Academic\LectureNotes\LectureNoteSearchExtensions.cs`
**Hash**: `163091eab179` | **Size**: 389 chars

**Classes**: LectureNoteSearchExtensions
```cs
using eNote.Application.Common.Search;
using eNote.Domain.Entities;

namespace eNote.Application.Features.Academic.LectureNotes;

public static class LectureNoteSearchExtensions
{
    public static IQueryable<LectureNote> ApplySearch(this IQueryable<LectureNote> query, LectureNoteSearchObject search) =>
        query.WhereContainsIf(search.Title, x => x.Title.Contains(search.Title!));
}
```

---

## File: `eNote\eNote.Application\Features\Academic\LectureNotes\LectureNoteSearchObject.cs`
**Hash**: `f3e450e7d623` | **Size**: 200 chars

**Classes**: LectureNoteSearchObject
```cs
using eNote.Application.Common.Search;

namespace eNote.Application.Features.Academic.LectureNotes;

public class LectureNoteSearchObject : BaseSearchObject
{
    public string? Title { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\LectureNotes\Services\ILectureNoteService.cs`
**Hash**: `c4a44bcda5d1` | **Size**: 795 chars

**Classes**: 
**Interfaces**: ILectureNoteService
```cs
using eNote.Application.Common.Paging;
using eNote.Application.Features.Academic.LectureNotes;

namespace eNote.Application.Features.Academic.LectureNotes.Services;

public interface ILectureNoteService
{
    Task<PagedResult<LectureNoteDto>> GetForLectureAsync(int lectureId, LectureNoteSearchObject search);
    Task<LectureNoteDto> GetByIdForInstructorAsync(int lectureId, int noteId);
    Task<LectureNoteDto> CreateAsync(int lectureId, LectureNoteRequest request);
    Task<LectureNoteDto> UpdateAsync(int lectureId, int noteId, LectureNoteRequest request);
    Task DeleteAsync(int lectureId, int noteId);
    Task<PagedResult<LectureNoteDto>> GetForStudentAsync(int lectureId, LectureNoteSearchObject search);
    Task<LectureNoteDto> GetByIdForStudentAsync(int lectureId, int noteId);
}

```

---

## File: `eNote\eNote.Application\Features\Academic\LectureNotes\Services\LectureNoteService.cs`
**Hash**: `5b39e1171be2` | **Size**: 3983 chars

**Classes**: LectureNoteService
### Key Cross-Cutting Interactions
- Uses **IInstructorAccessService** → Instructor ownership enforcement
- Uses **ICurrentActor** → Current actor resolution
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Academic.LectureNotes;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Academic.Courses;
using eNote.Domain.Entities;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Academic.LectureNotes.Services;

public sealed class LectureNoteService(
    IAppDbContext context,
    ICurrentActor actor,
    IInstructorAccessService instructorAccess,
    IMapper mapper) : ILectureNoteService
{
    public async Task<PagedResult<LectureNoteDto>> GetForLectureAsync(int lectureId, LectureNoteSearchObject search)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);

        var query = instructorAccess.LectureNotesForLecture(lectureId, instructorId)
            .AsNoTracking()
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<LectureNoteDto>, q => q.OrderByDescending(x => x.CreatedAt));
    }

    public async Task<LectureNoteDto> GetByIdForInstructorAsync(int lectureId, int noteId) =>
        mapper.Map<LectureNoteDto>(await GetOwnedNoteAsync(lectureId, noteId));

    public async Task<LectureNoteDto> CreateAsync(int lectureId, LectureNoteRequest request)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        await instructorAccess.EnsureOwnsLectureAsync(lectureId, instructorId);

        var entity = new LectureNote(request.Title.Trim(), request.Content.Trim(), lectureId)
        {
            CreatedById = actor.UserId
        };

        context.Set<LectureNote>().Add(entity);
        await context.SaveChangesAsync();

        return mapper.Map<LectureNoteDto>(entity);
    }

    public async Task<LectureNoteDto> UpdateAsync(int lectureId, int noteId, LectureNoteRequest request)
    {
        var entity = await GetOwnedNoteAsync(lectureId, noteId, track: true);

        entity.UpdateDetails(request.Title.Trim(), request.Content.Trim());
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync();

        return mapper.Map<LectureNoteDto>(entity);
    }

    public async Task DeleteAsync(int lectureId, int noteId)
    {
        var entity = await GetOwnedNoteAsync(lectureId, noteId, track: true);

        entity.SoftDelete();
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync();
    }

    public async Task<PagedResult<LectureNoteDto>> GetForStudentAsync(int lectureId, LectureNoteSearchObject search)
    {
        var studentId = await actor.GetCurrentStudentIdAsync();

        var query = context.Set<LectureNote>()
            .AsNoTracking()
            .ForEnrolledStudent(studentId)
            .Where(x => x.LectureId == lectureId)
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<LectureNoteDto>, q => q.OrderByDescending(x => x.CreatedAt));
    }

    public async Task<LectureNoteDto> GetByIdForStudentAsync(int lectureId, int noteId)
    {
        var studentId = await actor.GetCurrentStudentIdAsync();

        var entity = await context.Set<LectureNote>()
            .AsNoTracking()
            .ForEnrolledStudent(studentId)
            .FirstOrDefaultAsync(x => x.Id == noteId && x.LectureId == lectureId)
            ?? throw new NotFoundException(Messages.LectureNoteNotFound);

        return mapper.Map<LectureNoteDto>(entity);
    }

    private async Task<LectureNote> GetOwnedNoteAsync(int lectureId, int noteId, bool track = false)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        return await instructorAccess.GetOwnedLectureNoteAsync(lectureId, noteId, instructorId, track);
    }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Lectures\AttendanceDto.cs`
**Hash**: `e70b16de25d9` | **Size**: 299 chars

**Classes**: AttendanceDto
```cs
using eNote.Domain.Enums;

namespace eNote.Application.Features.Academic.Lectures;

public class AttendanceDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }

    public string StudentName { get; set; } = null!;
    public AttendanceStatus AttendanceStatus { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Lectures\AttendanceSearchObject.cs`
**Hash**: `d954be278783` | **Size**: 163 chars

**Classes**: AttendanceSearchObject
```cs
using eNote.Application.Common.Search;

namespace eNote.Application.Features.Academic.Lectures;

public sealed class AttendanceSearchObject : BaseSearchObject
{
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Lectures\LectureCreateRequest.cs`
**Hash**: `d291f0087253` | **Size**: 594 chars

**Classes**: LectureCreateRequest
```cs
using eNote.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Academic.Lectures;

public class LectureCreateRequest
{
    [Required]
    public string Name { get; set; } = null!;
    [Required]
    public string Location { get; set; } = null!;
    [Required]
    public LectureType LectureType { get; set; }

    [Required]
    public DateTime LectureTime { get; set; }
    [Required]
    public int Duration { get; set; }
    public int? Capacity { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int CourseId { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Lectures\LectureDto.cs`
**Hash**: `05f977895b06` | **Size**: 553 chars

**Classes**: LectureDto
```cs
using eNote.Domain.Enums;

namespace eNote.Application.Features.Academic.Lectures;

public class LectureDto
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;
    public string Location { get; set; } = null!;
    public LectureType LectureType { get; set; }
    public LectureStatus LectureStatus { get; set; }
    public bool IsCancelled { get; set; }

    public DateTime LectureTime { get; set; }
    public int Duration { get; set; }
    public int? Capacity { get; set; }

    public int AttendeeCount { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Lectures\LectureMappingConfig.cs`
**Hash**: `5cd5fbd27523` | **Size**: 455 chars

**Classes**: LectureMappingConfig
```cs
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Mapster;

namespace eNote.Application.Features.Academic.Lectures;

public sealed class LectureMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Lecture, LectureDto>()
            .Map(dest => dest.AttendeeCount, src => src.Attendances == null ? 0 : src.Attendances.Count(a => a.AttendanceStatus == AttendanceStatus.Present));
    }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Lectures\LectureSearchExtensions.cs`
**Hash**: `7c0eca40e566` | **Size**: 722 chars

**Classes**: LectureSearchExtensions
```cs
using eNote.Application.Common.Search;
using eNote.Domain.Entities;

namespace eNote.Application.Features.Academic.Lectures;

public static class LectureSearchExtensions
{
    public static IQueryable<Lecture> ApplySearch(this IQueryable<Lecture> query, LectureSearchObject search) =>
        query
            .WhereContainsIf(search.Name, x => x.Name.Contains(search.Name!))
            .WhereEqualsIf(search.LectureType, x => x.LectureType == search.LectureType!.Value)
            .WhereEqualsIf(search.CourseId, x => x.CourseId == search.CourseId!.Value)
            .WhereEqualsIf(search.From, x => x.LectureTime >= search.From!.Value)
            .WhereEqualsIf(search.To, x => x.LectureTime <= search.To!.Value);
}
```

---

## File: `eNote\eNote.Application\Features\Academic\Lectures\LectureSearchObject.cs`
**Hash**: `fceee39d1ab3` | **Size**: 385 chars

**Classes**: LectureSearchObject
```cs
using eNote.Application.Common.Search;
using eNote.Domain.Enums;

namespace eNote.Application.Features.Academic.Lectures;

public class LectureSearchObject : BaseSearchObject
{
    public int? CourseId { get; set; }
    public string? Name { get; set; }
    public LectureType? LectureType { get; set; }

    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Lectures\LectureUpdateRequest.cs`
**Hash**: `cc480215933c` | **Size**: 421 chars

**Classes**: LectureUpdateRequest
```cs
using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Academic.Lectures;

public class LectureUpdateRequest
{
    [Required]
    public string Name { get; set; } = null!;
    [Required]
    public string Location { get; set; } = null!;

    [Required]
    public DateTime LectureTime { get; set; }
    [Required]
    public int Duration { get; set; }
    public int? Capacity { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Lectures\MarkAttendanceRequest.cs`
**Hash**: `2b7339a16da2` | **Size**: 310 chars

**Classes**: MarkAttendanceRequest
```cs
using eNote.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Academic.Lectures;

public class MarkAttendanceRequest
{
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }
    [Required]
    public AttendanceStatus AttendanceStatus { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Lectures\RsvpRequest.cs`
**Hash**: `1ba303e2558f` | **Size**: 223 chars

**Classes**: RsvpRequest
```cs
using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Academic.Lectures;

public class RsvpRequest
{
    [Required]
    public bool Confirm { get; set; }
    public string? Note { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Lectures\RsvpResponse.cs`
**Hash**: `6908aa05a55c` | **Size**: 206 chars

**Classes**: RsvpResponse
```cs
namespace eNote.Application.Features.Academic.Lectures;

public class RsvpResponse
{
    public int LectureId { get; set; }
    public int StudentId { get; set; }

    public bool Confirmed { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Lectures\Services\ILectureAttendanceService.cs`
**Hash**: `3edd160ac857` | **Size**: 417 chars

**Classes**: 
**Interfaces**: ILectureAttendanceService
```cs
using eNote.Application.Common.Paging;

namespace eNote.Application.Features.Academic.Lectures.Services;

public interface ILectureAttendanceService
{
    Task<RsvpResponse> RsvpAsync(int lectureId, RsvpRequest request);
    Task<AttendanceDto> MarkAttendanceAsync(int lectureId, MarkAttendanceRequest request);
    Task<PagedResult<AttendanceDto>> GetAttendanceAsync(int lectureId, AttendanceSearchObject search);
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Lectures\Services\ILectureService.cs`
**Hash**: `124338b7f247` | **Size**: 689 chars

**Classes**: 
**Interfaces**: ILectureService
```cs
using eNote.Application.Common.Paging;
using eNote.Application.Features.Academic.Lectures;

namespace eNote.Application.Features.Academic.Lectures.Services;

public interface ILectureService
{
    Task<PagedResult<LectureDto>> GetPagedForInstructorAsync(LectureSearchObject search);
    Task<PagedResult<LectureDto>> GetPagedForStudentAsync(LectureSearchObject search);
    Task<LectureDto> GetByIdForInstructorAsync(int id);
    Task<LectureDto> GetByIdForStudentAsync(int id);
    Task<LectureDto> CreateAsync(LectureCreateRequest request);
    Task<LectureDto> UpdateAsync(int id, LectureUpdateRequest request);
    Task DeleteAsync(int id);
    Task<LectureDto> CancelAsync(int id);
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Lectures\Services\LectureAttendanceService.cs`
**Hash**: `6b0a671e7eed` | **Size**: 5568 chars

**Classes**: LectureAttendanceService
### Key Cross-Cutting Interactions
- Uses **IInstructorAccessService** → Instructor ownership enforcement
- Uses **ICurrentActor** → Current actor resolution
- Uses **IStudentDisplayNameService** → Student display-name formatting
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Academic.Courses;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Academic.Lectures.Services;

public sealed class LectureAttendanceService(IAppDbContext context, ICurrentActor actor, IStudentDisplayNameService displayNames, IInstructorAccessService instructorAccess, ILogger<LectureAttendanceService> logger) : ILectureAttendanceService
{
    public async Task<RsvpResponse> RsvpAsync(int lectureId, RsvpRequest request)
    {
        var lecture = await context.Set<Lecture>()
            .Include(x => x.Attendances)
            .Include(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == lectureId && x.Course.IsPublished && x.LectureStatus != LectureStatus.Cancelled) ?? throw new NotFoundException(Messages.LectureNotFound);

        if (lecture.IsCancelled)
        {
            throw new BusinessException(Messages.LectureCancelled);
        }

        var studentId = await actor.GetCurrentStudentIdAsync();

        if (!await context.IsEnrolledInCourseAsync(studentId, lecture.CourseId))
        {
            throw new BusinessException(Messages.StudentNotEnrolled);
        }

        var existing = lecture.Attendances.FirstOrDefault(x => x.StudentId == studentId);

        if (request.Confirm)
        {
            var confirmedCount = lecture.Attendances.Count(a => a.AttendanceStatus == AttendanceStatus.Present);

            if (lecture.Capacity.HasValue && confirmedCount >= lecture.Capacity.Value && (existing is null || existing.AttendanceStatus != AttendanceStatus.Present))
            {
                throw new ConflictException(Messages.LectureFull);
            }

            if (existing is null)
            {
                lecture.Attendances.Add(new Attendance(studentId, lecture.Id, AttendanceStatus.Present));
            }
            else
            {
                existing.UpdateStatus(AttendanceStatus.Present);
            }
        }
        else
        {
            existing?.UpdateStatus(AttendanceStatus.Absent);
        }

        try
        {
            context.Set<Lecture>().Entry(lecture).State = EntityState.Modified;
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Concurrency conflict while RSVPing for lecture {LectureId} by student user {StudentUserId}", lectureId, actor.UserId);
            throw new ConflictException(Messages.LectureRsvpConflict);
        }

        return new RsvpResponse { LectureId = lecture.Id, StudentId = studentId, Confirmed = request.Confirm };
    }

    public async Task<PagedResult<AttendanceDto>> GetAttendanceAsync(int lectureId, AttendanceSearchObject search)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        await instructorAccess.EnsureOwnsLectureAsync(lectureId, instructorId);

        var query = context.Set<Attendance>()
            .AsNoTracking()
            .Include(x => x.Student)
            .Where(x => x.LectureId == lectureId);

        return await query.ToPagedResultAsync(search, items => displayNames.GetStudentDisplayNamesAsync(items.Select(a => a.Student)), (a, names) => new AttendanceDto
        {
            Id = a.Id,
            StudentId = a.StudentId,
            StudentName = names.GetValueOrDefault(a.StudentId, $"Student {a.StudentId}"),
            AttendanceStatus = a.AttendanceStatus
        }, q => q.OrderBy(x => x.StudentId));
    }

    public async Task<AttendanceDto> MarkAttendanceAsync(int lectureId, MarkAttendanceRequest request)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        var lecture = await instructorAccess.GetOwnedLectureAsync(lectureId, instructorId, track: true, includeAttendances: true);

        if (lecture.IsCancelled)
        {
            throw new BusinessException(Messages.LectureCancelled);
        }

        if (!await context.IsEnrolledInCourseAsync(request.StudentId, lecture.CourseId))
        {
            throw new BusinessException(Messages.StudentNotEnrolled);
        }

        var attendance = lecture.Attendances.FirstOrDefault(x => x.StudentId == request.StudentId);

        if (attendance is null)
        {
            attendance = new Attendance(request.StudentId, lecture.Id, request.AttendanceStatus)
            {
                CreatedById = actor.UserId
            };
            lecture.Attendances.Add(attendance);
        }
        else
        {
            attendance.UpdateStatus(request.AttendanceStatus);
            attendance.UpdatedById = actor.UserId;
        }

        await context.SaveChangesAsync();

        var student = attendance.Student ?? await context.Set<Student>()
            .AsNoTracking()
            .FirstAsync(x => x.Id == attendance.StudentId);

        return new AttendanceDto
        {
            Id = attendance.Id,
            StudentId = attendance.StudentId,
            StudentName = await displayNames.GetStudentDisplayNameAsync(student),
            AttendanceStatus = attendance.AttendanceStatus
        };
    }
}

```

---

## File: `eNote\eNote.Application\Features\Academic\Lectures\Services\LectureService.cs`
**Hash**: `a2e73aa9838f` | **Size**: 5055 chars

**Classes**: LectureService
### Key Cross-Cutting Interactions
- Uses **IInstructorAccessService** → Instructor ownership enforcement
- Uses **ICurrentActor** → Current actor resolution
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Academic.Lectures;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Academic.Courses;
using eNote.Domain.Entities;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Academic.Lectures.Services;

public sealed class LectureService(
    IAppDbContext context,
    ICurrentActor actor,
    IInstructorAccessService instructorAccess,
    ILogger<LectureService> logger,
    IMapper mapper) : ILectureService
{
    public async Task<LectureDto> GetByIdForInstructorAsync(int id)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        var entity = await instructorAccess.GetOwnedLectureAsync(id, instructorId, includeAttendances: true);
        return mapper.Map<LectureDto>(entity);
    }

    public async Task<LectureDto> GetByIdForStudentAsync(int id)
    {
        var studentId = await actor.GetCurrentStudentIdAsync();

        var entity = await context.Set<Lecture>()
            .Include(x => x.Attendances)
            .AsNoTracking()
            .ForEnrolledStudent(studentId)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException(Messages.LectureNotFound);

        return mapper.Map<LectureDto>(entity);
    }

    public async Task<PagedResult<LectureDto>> GetPagedForInstructorAsync(LectureSearchObject search)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);

        var query = instructorAccess.LecturesFor(instructorId)
            .AsNoTracking()
            .Include(x => x.Attendances)
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<LectureDto>, q => q.OrderByDescending(x => x.LectureTime));
    }

    public async Task<PagedResult<LectureDto>> GetPagedForStudentAsync(LectureSearchObject search)
    {
        var studentId = await actor.GetCurrentStudentIdAsync();

        var query = context.Set<Lecture>()
            .AsNoTracking()
            .Include(x => x.Attendances)
            .ForEnrolledStudent(studentId)
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<LectureDto>, q => q.OrderByDescending(x => x.LectureTime));
    }

    public async Task<LectureDto> CreateAsync(LectureCreateRequest request)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        await instructorAccess.EnsureOwnsCourseAsync(request.CourseId, instructorId);

        var entity = new Lecture(
            request.Name.Trim(),
            request.Location.Trim(),
            request.Duration,
            request.LectureTime,
            request.LectureType,
            request.Capacity,
            request.CourseId)
        {
            CreatedById = actor.UserId
        };

        context.Set<Lecture>().Add(entity);
        await context.SaveChangesAsync();

        return mapper.Map<LectureDto>(entity);
    }

    public async Task<LectureDto> UpdateAsync(int id, LectureUpdateRequest request)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        var entity = await instructorAccess.GetOwnedLectureAsync(id, instructorId, track: true);

        if (entity.IsCancelled)
        {
            throw new BusinessException(Messages.LectureCancelled);
        }

        entity.UpdateDetails(
            request.Name.Trim(),
            request.Location.Trim(),
            request.Duration,
            request.LectureTime,
            request.Capacity);
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync();

        return mapper.Map<LectureDto>(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        var entity = await instructorAccess.GetOwnedLectureAsync(id, instructorId, track: true);

        entity.SoftDelete();
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync();

        logger.LogInformation("Lecture {LectureId} soft-deleted by instructor user {InstructorUserId}", id, actor.UserId);
    }

    public async Task<LectureDto> CancelAsync(int id)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        var entity = await instructorAccess.GetOwnedLectureAsync(id, instructorId, track: true);

        if (entity.IsCancelled)
        {
            throw new BusinessException(Messages.LectureCancelled);
        }

        entity.Cancel();
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync();

        return mapper.Map<LectureDto>(entity);
    }
}

```

---

## File: `eNote\eNote.Application\Features\Communication\Announcements\AnnouncementDto.cs`
**Hash**: `b7d464e1971f` | **Size**: 564 chars

**Classes**: AnnouncementDto
```cs
using eNote.Domain.Enums;

namespace eNote.Application.Features.Communication.Announcements;

public class AnnouncementDto
{
    public int Id { get; set; }
    public int? CourseId { get; set; }
    public int? MusicStoreId { get; set; }

    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string? ImagePath { get; set; }
    public AnnouncementScope Scope { get; set; }
    public string? CourseName { get; set; }
    public string? StoreName { get; set; }

    public DateTime PublishedAt { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Communication\Announcements\AnnouncementMappingConfig.cs`
**Hash**: `e332170cb4dc` | **Size**: 647 chars

**Classes**: AnnouncementMappingConfig
```cs
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Mapster;

namespace eNote.Application.Features.Communication.Announcements;

public sealed class AnnouncementMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Announcement, AnnouncementDto>()
            .Map(dest => dest.Scope, src => src.CourseId.HasValue ? AnnouncementScope.Course : AnnouncementScope.MusicStore)
            .Map(dest => dest.CourseName, src => src.Course == null ? null : src.Course.Name)
            .Map(dest => dest.StoreName, src => src.MusicStore == null ? null : src.MusicStore.StoreName);
    }
}

```

---

## File: `eNote\eNote.Application\Features\Communication\Announcements\AnnouncementRequest.cs`
**Hash**: `db3332ca8bb1` | **Size**: 227 chars

```cs
using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Communication.Announcements;

public sealed record AnnouncementRequest([property: Required] string Title, [property: Required] string Content);

```

---

## File: `eNote\eNote.Application\Features\Communication\Announcements\AnnouncementSearchObject.cs`
**Hash**: `a1c840d53bba` | **Size**: 175 chars

**Classes**: AnnouncementSearchObject
```cs
using eNote.Application.Common.Search;

namespace eNote.Application.Features.Communication.Announcements;

public sealed class AnnouncementSearchObject : BaseSearchObject
{
}

```

---

## File: `eNote\eNote.Application\Features\Communication\Announcements\Services\AnnouncementService.cs`
**Hash**: `3001518bec58` | **Size**: 8400 chars

**Classes**: AnnouncementService
### Key Cross-Cutting Interactions
- Uses **IInstructorAccessService** → Instructor ownership enforcement
- Uses **ICurrentActor** → Current actor resolution
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Communication.Announcements;
using eNote.Application.Features.Identity.Instructors;
using eNote.Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Communication.Announcements.Services;

public sealed class AnnouncementService(IAppDbContext context, IClock clock, ICurrentActor actor, IInstructorAccessService instructorAccess, IFileStorageService fileStorage, IMapper mapper)
     : ICourseAnnouncementService, IStoreAnnouncementService, IStudentAnnouncementService
{
    public async Task<PagedResult<AnnouncementDto>> GetFeedForStudentAsync(AnnouncementSearchObject search)
    {
        var studentId = await actor.GetCurrentStudentIdAsync();

        var query = context.Set<Announcement>()
            .AsNoTracking()
            .Include(a => a.Course)
            .Include(a => a.MusicStore)
            .Where(a =>
                (a.CourseId != null && context.Set<Enrollment>().Any(e =>
                    e.StudentId == studentId &&
                    e.EnrollmentStatus == EnrollmentStatus.Active &&
                    e.CourseId == a.CourseId)) ||
                (a.MusicStoreId != null && context.Set<InstrumentRental>().Any(r =>
                    r.StudentProfileId == studentId &&
                    InstrumentRentalStatusSets.History.Contains(r.RentalStatus) &&
                    r.Instrument.MusicStoreId == a.MusicStoreId)));

        return await query.ToPagedResultAsync(search, mapper.Map<AnnouncementDto>, q => q.OrderByDescending(x => x.PublishedAt));
    }

    public async Task<AnnouncementDto> CreateForCourseAsync(int courseId, AnnouncementRequest request)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);

        if (!await instructorAccess.OwnsCourseAsync(courseId, instructorId))
        {
            throw new BusinessException(Messages.AnnouncementCourseForbidden);
        }

        var entity = new Announcement(request.Title.Trim(), request.Content.Trim(), courseId, null, clock.UtcNow)
        {
            CreatedById = actor.UserId
        };

        context.Set<Announcement>().Add(entity);
        await context.SaveChangesAsync();

        return mapper.Map<AnnouncementDto>(entity);
    }

    public async Task<AnnouncementDto> GetByIdForCourseAsync(int courseId, int announcementId)
    {
        var entity = await (await GetCourseAnnouncementQueryAsync(courseId)).FirstOrDefaultAsync(a => a.Id == announcementId) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

        return mapper.Map<AnnouncementDto>(entity);
    }

    public async Task<PagedResult<AnnouncementDto>> GetForCourseAsync(int courseId, AnnouncementSearchObject search)
    {
        return await (await GetCourseAnnouncementQueryAsync(courseId)).ToPagedResultAsync(search, mapper.Map<AnnouncementDto>, q => q.OrderByDescending(x => x.PublishedAt));
    }

    public async Task<AnnouncementDto> UpdateForCourseAsync(int courseId, int announcementId, AnnouncementRequest request)
    {
        var entity = await (await GetCourseAnnouncementQueryAsync(courseId, track: true)).FirstOrDefaultAsync(a => a.Id == announcementId) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

        entity.UpdateDetails(request.Title.Trim(), request.Content.Trim());
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync();

        return mapper.Map<AnnouncementDto>(entity);
    }

    public async Task DeleteForCourseAsync(int courseId, int announcementId)
    {
        var entity = await (await GetCourseAnnouncementQueryAsync(courseId, track: true)).FirstOrDefaultAsync(a => a.Id == announcementId) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

        entity.SoftDelete();
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync();
    }

    public async Task<AnnouncementDto> CreateForStoreAsync(AnnouncementRequest request)
    {
        var storeId = await actor.GetCurrentStoreIdAsync();

        var entity = new Announcement(request.Title.Trim(), request.Content.Trim(), null, storeId, clock.UtcNow)
        {
            CreatedById = actor.UserId
        };

        context.Set<Announcement>().Add(entity);
        await context.SaveChangesAsync();

        return mapper.Map<AnnouncementDto>(entity);
    }

    public async Task<AnnouncementDto> GetByIdForStoreAsync(int announcementId)
    {
        var storeId = await actor.GetCurrentStoreIdAsync();

        var entity = await context.Set<Announcement>()
            .AsNoTracking()
            .Include(a => a.MusicStore)
            .FirstOrDefaultAsync(a => a.Id == announcementId && a.MusicStoreId == storeId) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

        return mapper.Map<AnnouncementDto>(entity);
    }

    public async Task<PagedResult<AnnouncementDto>> GetForStoreAsync(AnnouncementSearchObject search)
    {
        var storeId = await actor.GetCurrentStoreIdAsync();

        return await context.Set<Announcement>()
            .AsNoTracking()
            .Include(a => a.MusicStore)
            .Where(a => a.MusicStoreId == storeId)
            .ToPagedResultAsync(search, mapper.Map<AnnouncementDto>, q => q.OrderByDescending(x => x.PublishedAt));
    }

    public async Task<AnnouncementDto> UpdateForStoreAsync(int announcementId, AnnouncementRequest request)
    {
        var storeId = await actor.GetCurrentStoreIdAsync();

        var entity = await context.Set<Announcement>()
            .FirstOrDefaultAsync(a => a.Id == announcementId && a.MusicStoreId == storeId) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

        entity.UpdateDetails(request.Title.Trim(), request.Content.Trim());
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync();

        return mapper.Map<AnnouncementDto>(entity);
    }

    public async Task DeleteForStoreAsync(int announcementId)
    {
        var storeId = await actor.GetCurrentStoreIdAsync();

        var entity = await context.Set<Announcement>()
            .FirstOrDefaultAsync(a => a.Id == announcementId && a.MusicStoreId == storeId) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

        entity.SoftDelete();
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync();
    }

    public async Task<AnnouncementDto> UploadImageForCourseAsync(int courseId, int announcementId, Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        var entity = await (await GetCourseAnnouncementQueryAsync(courseId, track: true)).FirstOrDefaultAsync(a => a.Id == announcementId, ct) ?? throw new NotFoundException(Messages.AnnouncementNotFound);
        var path = await fileStorage.SaveAsync(stream, fileName, contentType, "announcements", ct);

        entity.SetImagePath(path);
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync(ct);

        return mapper.Map<AnnouncementDto>(entity);
    }

    public async Task<AnnouncementDto> UploadImageForStoreAsync(int announcementId, Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        var storeId = await actor.GetCurrentStoreIdAsync();

        var entity = await context.Set<Announcement>()
            .FirstOrDefaultAsync(a => a.Id == announcementId && a.MusicStoreId == storeId, ct) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

        var path = await fileStorage.SaveAsync(stream, fileName, contentType, "announcements", ct);

        entity.SetImagePath(path);
        entity.UpdatedById = actor.UserId;

        await context.SaveChangesAsync(ct);

        return mapper.Map<AnnouncementDto>(entity);
    }

    private async Task<IQueryable<Announcement>> GetCourseAnnouncementQueryAsync(int courseId, bool track = false)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);

        return instructorAccess.CourseAnnouncementsFor(courseId, instructorId, track);
    }
}

```

---

## File: `eNote\eNote.Application\Features\Communication\Announcements\Services\ICourseAnnouncementService.cs`
**Hash**: `ee6ac41cf309` | **Size**: 852 chars

**Classes**: 
**Interfaces**: ICourseAnnouncementService
```cs
using eNote.Application.Common.Paging;
using eNote.Application.Features.Communication.Announcements;

namespace eNote.Application.Features.Communication.Announcements.Services;

public interface ICourseAnnouncementService
{
    Task<PagedResult<AnnouncementDto>> GetForCourseAsync(int courseId, AnnouncementSearchObject search);
    Task<AnnouncementDto> GetByIdForCourseAsync(int courseId, int announcementId);
    Task<AnnouncementDto> CreateForCourseAsync(int courseId, AnnouncementRequest request);
    Task<AnnouncementDto> UpdateForCourseAsync(int courseId, int announcementId, AnnouncementRequest request);
    Task DeleteForCourseAsync(int courseId, int announcementId);
    Task<AnnouncementDto> UploadImageForCourseAsync(int courseId, int announcementId, Stream stream, string fileName, string contentType, CancellationToken ct = default);
}

```

---

## File: `eNote\eNote.Application\Features\Communication\Announcements\Services\IStoreAnnouncementService.cs`
**Hash**: `8dda14d7e4fa` | **Size**: 761 chars

**Classes**: 
**Interfaces**: IStoreAnnouncementService
```cs
using eNote.Application.Common.Paging;
using eNote.Application.Features.Communication.Announcements;

namespace eNote.Application.Features.Communication.Announcements.Services;

public interface IStoreAnnouncementService
{
    Task<PagedResult<AnnouncementDto>> GetForStoreAsync(AnnouncementSearchObject search);
    Task<AnnouncementDto> GetByIdForStoreAsync(int announcementId);
    Task<AnnouncementDto> CreateForStoreAsync(AnnouncementRequest request);
    Task<AnnouncementDto> UpdateForStoreAsync(int announcementId, AnnouncementRequest request);
    Task DeleteForStoreAsync(int announcementId);
    Task<AnnouncementDto> UploadImageForStoreAsync(int announcementId, Stream stream, string fileName, string contentType, CancellationToken ct = default);
}

```

---

## File: `eNote\eNote.Application\Features\Communication\Announcements\Services\IStudentAnnouncementService.cs`
**Hash**: `6422344bb2c2` | **Size**: 323 chars

**Classes**: 
**Interfaces**: IStudentAnnouncementService
```cs
using eNote.Application.Common.Paging;
using eNote.Application.Features.Communication.Announcements;

namespace eNote.Application.Features.Communication.Announcements.Services;

public interface IStudentAnnouncementService
{
    Task<PagedResult<AnnouncementDto>> GetFeedForStudentAsync(AnnouncementSearchObject search);
}

```

---

## File: `eNote\eNote.Application\Features\Communication\Notifications\NotificationDto.cs`
**Hash**: `4c429c5ba45f` | **Size**: 347 chars

**Classes**: NotificationDto
```cs
namespace eNote.Application.Features.Communication.Notifications;

public class NotificationDto
{
    public int Id { get; set; }
    public int? RentalId { get; set; }

    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Communication\Notifications\NotificationPushDto.cs`
**Hash**: `5be36f9e6cf4` | **Size**: 284 chars

**Classes**: NotificationPushDto
```cs
namespace eNote.Application.Features.Communication.Notifications;

public class NotificationPushDto
{
    public int? RentalId { get; init; }
    public string Title { get; init; } = null!;
    public string Body { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
}

```

---

## File: `eNote\eNote.Application\Features\Communication\Notifications\NotificationSearchExtensions.cs`
**Hash**: `0882f9c08f46` | **Size**: 399 chars

**Classes**: NotificationSearchExtensions
```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Search;

namespace eNote.Application.Features.Communication.Notifications;

public static class NotificationSearchExtensions
{
    public static IQueryable<Notification> ApplySearch(this IQueryable<Notification> query, NotificationSearchObject search) =>
        query.WhereEqualsIf(search.IsRead, x => x.IsRead == search.IsRead!.Value);
}
```

---

## File: `eNote\eNote.Application\Features\Communication\Notifications\NotificationSearchObject.cs`
**Hash**: `4d1f4a81baea` | **Size**: 206 chars

**Classes**: NotificationSearchObject
```cs
using eNote.Application.Common.Search;

namespace eNote.Application.Features.Communication.Notifications;

public class NotificationSearchObject : BaseSearchObject
{
    public bool? IsRead { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Communication\Notifications\NotificationUnreadCountDto.cs`
**Hash**: `bae0d7965f33` | **Size**: 153 chars

**Classes**: NotificationUnreadCountDto
```cs
namespace eNote.Application.Features.Communication.Notifications;

public class NotificationUnreadCountDto
{
    public int UnreadCount { get; init; }
}

```

---

## File: `eNote\eNote.Application\Features\Communication\Notifications\Services\INotificationService.cs`
**Hash**: `b2dbd04c9e63` | **Size**: 660 chars

**Classes**: 
**Interfaces**: INotificationService
```cs
using eNote.Application.Common.Paging;
using eNote.Application.Features.Communication.Notifications;

namespace eNote.Application.Features.Communication.Notifications.Services;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetPagedAsync(NotificationSearchObject search, CancellationToken cancellationToken = default);

    Task<NotificationUnreadCountDto> GetUnreadCountAsync(CancellationToken cancellationToken = default);

    Task<NotificationDto> MarkReadAsync(int id, CancellationToken cancellationToken = default);

    Task<NotificationUnreadCountDto> MarkAllReadAsync(CancellationToken cancellationToken = default);
}

```

---

## File: `eNote\eNote.Application\Features\Communication\Notifications\Services\NotificationService.cs`
**Hash**: `c4d2b4066f58` | **Size**: 2623 chars

**Classes**: NotificationService
### Key Cross-Cutting Interactions
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Communication.Notifications;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Communication.Notifications.Services;

public sealed class NotificationService(IAppDbContext context, IMapper mapper, ICurrentUserService currentUserService) : INotificationService
{
    public async Task<PagedResult<NotificationDto>> GetPagedAsync(NotificationSearchObject search, CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;

        var query = context.Set<Notification>()
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        query = query.ApplySearch(search);

        return await query.ToPagedResultAsync(
            search,
            mapper.Map<NotificationDto>,
            q => q.OrderByDescending(x => x.CreatedAt),
            cancellationToken);
    }

    public async Task<NotificationUnreadCountDto> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;

        var count = await context.Set<Notification>()
            .AsNoTracking()
            .CountAsync(x => x.UserId == userId && !x.IsRead, cancellationToken);

        return new NotificationUnreadCountDto { UnreadCount = count };
    }

    public async Task<NotificationDto> MarkReadAsync(int id, CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;

        var notification = await context.Set<Notification>()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(Messages.NotificationNotFound);

        if (!notification.IsRead)
        {
            notification.MarkRead();
            await context.SaveChangesAsync(cancellationToken);
        }

        return mapper.Map<NotificationDto>(notification);
    }

    public async Task<NotificationUnreadCountDto> MarkAllReadAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId;

        await context.Set<Notification>()
            .Where(x => x.UserId == userId && !x.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsRead, true), cancellationToken);

        return await GetUnreadCountAsync(cancellationToken);
    }
}

```

---

## File: `eNote\eNote.Application\Features\Files\Services\FileAccessService.cs`
**Hash**: `22595b2f4540` | **Size**: 1847 chars

**Classes**: FileAccessService
### Key Cross-Cutting Interactions
- Uses **IInstructorAccessService** → Instructor ownership enforcement
- Uses **IUserProfileLookup** → User profile lookup
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Files.Services;

public sealed class FileAccessService(
    IAppDbContext context,
    IUserProfileLookup lookup,
    IInstructorAccessService instructorAccess,
    IUserIdentityService identity) : IFileAccessService
{
    public async Task<bool> CanAccessAssignmentFileAsync(int userId, string fileName, CancellationToken cancellationToken = default)
    {
        var apiPath = $"/api/uploads/assignments/{fileName}";
        var legacyPath = $"/uploads/assignments/{fileName}";

        var submission = await context.Set<AssignmentSubmission>()
            .AsNoTracking()
            .Include(x => x.Assignment)
                .ThenInclude(x => x.Lecture)
                .ThenInclude(x => x.Course)
            .Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.FilePath == apiPath || x.FilePath == legacyPath, cancellationToken);

        if (submission is null)
        {
            return false;
        }

        var roles = await identity.GetRolesAsync(userId);

        if (roles.Contains(AppRoles.Administrator))
        {
            return true;
        }

        if (roles.Contains(AppRoles.Student))
        {
            var student = await lookup.GetStudentAsync(userId);
            return submission.StudentId == student.Id;
        }

        if (roles.Contains(AppRoles.Instructor))
        {
            var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(userId);
            return submission.Assignment.Lecture.Course.InstructorId == instructorId;
        }

        return false;
    }
}
```

---

## File: `eNote\eNote.Application\Features\Files\Services\IFileAccessService.cs`
**Hash**: `7453989735ab` | **Size**: 214 chars

**Classes**: 
**Interfaces**: IFileAccessService
```cs
namespace eNote.Application.Features.Files.Services;

public interface IFileAccessService
{
    Task<bool> CanAccessAssignmentFileAsync(int userId, string fileName, CancellationToken cancellationToken = default);
}
```

---

## File: `eNote\eNote.Application\Features\Identity\Instructors\AdminInstructorService.cs`
**Hash**: `477b0ac73ca5` | **Size**: 2740 chars

**Classes**: AdminInstructorService
### Key Cross-Cutting Interactions
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Identity.Instructors;

public sealed class AdminInstructorService(IAppDbContext context, IUserIdentityService identityService) : IAdminInstructorService
{
    public async Task<PagedResult<InstructorDto>> GetPagedAsync(InstructorSearchObject search)
    {
        IQueryable<Instructor> query = context.Set<Instructor>()
            .AsNoTracking()
            .OrderBy(x => x.Id);

        List<Instructor> instructors = await query.ToListAsync();
        IReadOnlyDictionary<int, UserIdentityDto> users = await identityService.GetUsersBulkAsync(instructors.Select(x => x.AppUserId));

        List<InstructorDto> filtered = [.. instructors
            .Select(x => Map(x, users.GetValueOrDefault(x.AppUserId)))
            .Where(x => MatchesName(x, search.Name))];

        (var page, var pageSize) = PagingLimits.Normalize(search.Page, search.PageSize);

        return new PagedResult<InstructorDto>
        {
            Items = [.. filtered.Skip((page - 1) * pageSize).Take(pageSize)],
            Page = page,
            PageSize = pageSize,
            TotalCount = search.IncludeTotalCount ? filtered.Count : null
        };
    }

    public async Task<InstructorDto> GetByIdAsync(int id)
    {
        Instructor entity = await context.Set<Instructor>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id) ?? throw new NotFoundException(Messages.InstructorProfileNotFound);

        UserIdentityDto? user = await identityService.GetUserAsync(entity.AppUserId);

        return Map(entity, user);
    }

    private static InstructorDto Map(Instructor entity, UserIdentityDto? user) => new()
    {
        Id = entity.Id,
        AppUserId = entity.AppUserId,
        FirstName = user?.FirstName,
        LastName = user?.LastName,
        Username = user?.Username
    };

    private static bool MatchesName(InstructorDto dto, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return true;
        }

        var fullName = $"{dto.FirstName} {dto.LastName}".Trim();

        return Contains(dto.FirstName, name)
            || Contains(dto.LastName, name)
            || Contains(dto.Username, name)
            || Contains(fullName, name);
    }

    private static bool Contains(string? value, string name) => value?.Contains(name, StringComparison.OrdinalIgnoreCase) == true;
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Instructors\IAdminInstructorService.cs`
**Hash**: `f08d26c2e39b` | **Size**: 274 chars

**Classes**: 
**Interfaces**: IAdminInstructorService
```cs
using eNote.Application.Common.Paging;

namespace eNote.Application.Features.Identity.Instructors;

public interface IAdminInstructorService
{
    Task<PagedResult<InstructorDto>> GetPagedAsync(InstructorSearchObject search);
    Task<InstructorDto> GetByIdAsync(int id);
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Instructors\IInstructorAccessService.cs`
**Hash**: `63f9d42ec9b3` | **Size**: 1189 chars

**Classes**: 
**Interfaces**: IInstructorAccessService
### Key Cross-Cutting Interactions
- Uses **IInstructorAccessService** → Instructor ownership enforcement

```cs
using eNote.Domain.Entities;

namespace eNote.Application.Features.Identity.Instructors;

public interface IInstructorAccessService
{
    Task<Instructor> GetInstructorAsync(int userId);
    Task<int> GetCurrentInstructorIdAsync(int appUserId);

    Task<bool> OwnsCourseAsync(int courseId, int instructorId);

    Task EnsureOwnsCourseAsync(int courseId, int instructorId);

    Task EnsureOwnsLectureAsync(int lectureId, int instructorId);

    Task<Lecture> GetOwnedLectureAsync(int lectureId, int instructorId, bool track = false, bool includeAttendances = false);

    Task<Assignment> GetOwnedAssignmentAsync(int lectureId, int assignmentId, int instructorId, bool track = false);

    Task<LectureNote> GetOwnedLectureNoteAsync(int lectureId, int noteId, int instructorId, bool track = false);

    IQueryable<Course> CoursesFor(int instructorId);

    IQueryable<Lecture> LecturesFor(int instructorId);

    IQueryable<Assignment> AssignmentsForLecture(int lectureId, int instructorId);

    IQueryable<LectureNote> LectureNotesForLecture(int lectureId, int instructorId);

    IQueryable<Announcement> CourseAnnouncementsFor(int courseId, int instructorId, bool track = false);
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Instructors\InstructorAccessService.cs`
**Hash**: `c83d1ba35a8b` | **Size**: 3966 chars

**Classes**: InstructorAccessService
### Key Cross-Cutting Interactions
- Uses **IInstructorAccessService** → Instructor ownership enforcement
- Uses **IUserProfileLookup** → User profile lookup
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Identity.Users.Services;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Identity.Instructors;

public sealed class InstructorAccessService(IAppDbContext context, IUserProfileLookup lookup) : IInstructorAccessService
{
    public Task<Instructor> GetInstructorAsync(int userId) => lookup.GetInstructorAsync(userId);
    public async Task<int> GetCurrentInstructorIdAsync(int appUserId) => (await GetInstructorAsync(appUserId)).Id;

    public Task<bool> OwnsCourseAsync(int courseId, int instructorId) =>
        context.Set<Course>().AnyAsync(c => c.Id == courseId && c.InstructorId == instructorId);

    public async Task EnsureOwnsCourseAsync(int courseId, int instructorId)
    {
        if (!await OwnsCourseAsync(courseId, instructorId))
        {
            throw new AuthorizationException(Messages.CourseNotOwned);
        }
    }

    public async Task EnsureOwnsLectureAsync(int lectureId, int instructorId)
    {
        if (!await context.Set<Lecture>().AnyAsync(x => x.Id == lectureId && x.Course.InstructorId == instructorId))
        {
            throw new AuthorizationException(Messages.CourseNotOwned);
        }
    }

    public async Task<Lecture> GetOwnedLectureAsync(int lectureId, int instructorId, bool track = false, bool includeAttendances = false)
    {
        var query = context.Set<Lecture>()
            .Where(x => x.Id == lectureId && x.Course.InstructorId == instructorId);

        if (includeAttendances)
        {
            query = query.Include(x => x.Attendances);
        }

        query = track ? query : query.AsNoTracking();

        return await query.FirstOrDefaultAsync()
            ?? throw new NotFoundException(Messages.LectureNotFound);
    }

    public async Task<Assignment> GetOwnedAssignmentAsync(int lectureId, int assignmentId, int instructorId, bool track = false)
    {
        var query = context.Set<Assignment>()
            .Where(x => x.Id == assignmentId && x.LectureId == lectureId && x.Lecture.Course.InstructorId == instructorId);

        query = track ? query : query.AsNoTracking();

        return await query.FirstOrDefaultAsync()
            ?? throw new NotFoundException(Messages.AssignmentNotFound);
    }

    public async Task<LectureNote> GetOwnedLectureNoteAsync(int lectureId, int noteId, int instructorId, bool track = false)
    {
        var query = context.Set<LectureNote>()
            .Where(x => x.Id == noteId && x.LectureId == lectureId && x.Lecture.Course.InstructorId == instructorId);

        query = track ? query : query.AsNoTracking();

        return await query.FirstOrDefaultAsync()
            ?? throw new NotFoundException(Messages.LectureNoteNotFound);
    }

    public IQueryable<Course> CoursesFor(int instructorId) =>
        context.Set<Course>().Where(c => c.InstructorId == instructorId);

    public IQueryable<Lecture> LecturesFor(int instructorId) =>
        context.Set<Lecture>().Where(x => x.Course.InstructorId == instructorId);

    public IQueryable<Assignment> AssignmentsForLecture(int lectureId, int instructorId) =>
        context.Set<Assignment>().Where(x => x.LectureId == lectureId && x.Lecture.Course.InstructorId == instructorId);

    public IQueryable<LectureNote> LectureNotesForLecture(int lectureId, int instructorId) =>
        context.Set<LectureNote>().Where(x => x.LectureId == lectureId && x.Lecture.Course.InstructorId == instructorId);

    public IQueryable<Announcement> CourseAnnouncementsFor(int courseId, int instructorId, bool track = false)
    {
        var query = context.Set<Announcement>()
            .Include(a => a.Course)
            .Where(a => a.CourseId == courseId && a.Course!.InstructorId == instructorId);

        return track ? query : query.AsNoTracking();
    }
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Instructors\InstructorDto.cs`
**Hash**: `75f2b902e847` | **Size**: 290 chars

**Classes**: InstructorDto
```cs
namespace eNote.Application.Features.Identity.Instructors;

public class InstructorDto
{
    public int Id { get; set; }
    public int AppUserId { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Username { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Instructors\InstructorSearchObject.cs`
**Hash**: `f9f2120c0f5f` | **Size**: 204 chars

**Classes**: InstructorSearchObject
```cs
using eNote.Application.Common.Search;

namespace eNote.Application.Features.Identity.Instructors;

public sealed class InstructorSearchObject : BaseSearchObject
{
    public string? Name { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\AddressDto.cs`
**Hash**: `80d4dab82050` | **Size**: 228 chars

**Classes**: UserAddressDto
```cs
namespace eNote.Application.Features.Identity.Users;

public class UserAddressDto
{
    public string City { get; set; } = null!;
    public string Street { get; set; } = null!;
    public string Number { get; set; } = null!;
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\ChangePasswordRequest.cs`
**Hash**: `562d194e205a` | **Size**: 433 chars

**Classes**: ChangePasswordRequest
```cs
using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Identity.Users;

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = null!;

    [Required]
    public string NewPassword { get; set; } = null!;

    [Required]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmNewPassword { get; set; } = null!;
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\Profiles\AdminProfile.cs`
**Hash**: `fa102c712373` | **Size**: 143 chars

```cs
namespace eNote.Application.Features.Identity.Users.Profiles;

public record AdminProfile(string? FirstName, string? LastName) : IUserProfile;

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\Profiles\IUserProfile.cs`
**Hash**: `cb43c2af43ca` | **Size**: 97 chars

**Classes**: 
**Interfaces**: IUserProfile
```cs
namespace eNote.Application.Features.Identity.Users.Profiles;

public interface IUserProfile
{
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\Profiles\InstructorProfile.cs`
**Hash**: `0b99489eac3f` | **Size**: 156 chars

```cs
namespace eNote.Application.Features.Identity.Users.Profiles;

public record InstructorProfile(int Id, string? FirstName, string? LastName) : IUserProfile;

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\Profiles\MusicStoreProfile.cs`
**Hash**: `961183f7e433` | **Size**: 234 chars

```cs
using eNote.Application.Features.Identity.Users;

namespace eNote.Application.Features.Identity.Users.Profiles;

public record MusicStoreProfile(int Id, string StoreName, string BusinessHours, UserAddressDto? Address) : IUserProfile;

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\Profiles\StudentProfile.cs`
**Hash**: `390a84a6dcab` | **Size**: 307 chars

```cs
using eNote.Application.Features.Identity.Users;

namespace eNote.Application.Features.Identity.Users.Profiles;

public record StudentProfile(int Id, DateTime EnrollmentDate, string? FirstName, string? LastName, DateTime? DateOfBirth, UserAddressDto? Address, DateTime? MembershipPaidUntil) : IUserProfile;

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\Services\CurrentActor.cs`
**Hash**: `d6c57b492753` | **Size**: 1635 chars

**Classes**: CurrentActor
### Key Cross-Cutting Interactions
- Uses **ICurrentActor** → Current actor resolution
- Uses **IUserProfileLookup** → User profile lookup
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Identity.Users.Services;

public sealed class CurrentActor(ICurrentUserService user, IUserProfileLookup lookup, IAppDbContext context) : ICurrentActor
{
    private Student? _student;
    private Instructor? _instructor;
    private MusicStoreEmployee? _employee;
    private int? _storeId;

    public int UserId => user.UserId;
    public bool IsAuthenticated => user.IsAuthenticated;

    public async Task<Student> GetCurrentStudentAsync() => _student ??= await lookup.GetStudentAsync(user.UserId);
    public async Task<int> GetCurrentStudentIdAsync() => (await GetCurrentStudentAsync()).Id;
    public async Task<Instructor> GetCurrentInstructorAsync() => _instructor ??= await lookup.GetInstructorAsync(user.UserId);
    public async Task<MusicStoreEmployee> GetCurrentEmployeeAsync() => _employee ??= await lookup.GetActiveEmployeeAsync(user.UserId);

    public async Task<int> GetCurrentStoreIdAsync()
    {
        if (_storeId is not null) return _storeId.Value;

        var storeId = await context.Set<MusicStoreEmployee>()
            .AsNoTracking()
            .Where(x => x.AppUserId == user.UserId && x.IsActive)
            .Select(x => x.MusicStoreId)
            .SingleOrDefaultAsync();

        if (storeId == 0) throw new BusinessException(Messages.ActiveEmployeeStoreNotFound);

        return (_storeId = storeId).Value;
    }
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\Services\IStudentDisplayNameService.cs`
**Hash**: `997c18d61b32` | **Size**: 306 chars

**Classes**: 
**Interfaces**: IStudentDisplayNameService
### Key Cross-Cutting Interactions
- Uses **IStudentDisplayNameService** → Student display-name formatting

```cs
using eNote.Domain.Entities;

namespace eNote.Application.Features.Identity.Users.Services;

public interface IStudentDisplayNameService
{
    Task<string> GetStudentDisplayNameAsync(Student student);
    Task<IReadOnlyDictionary<int, string>> GetStudentDisplayNamesAsync(IEnumerable<Student> students);
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\Services\IUserAccountService.cs`
**Hash**: `5425cd9cde12` | **Size**: 953 chars

**Classes**: 
**Interfaces**: IUserAccountService
```cs
namespace eNote.Application.Features.Identity.Users.Services;

public interface IUserAccountService
{
    Task<int?> FindUserIdByUsernameAsync(string username);
    Task<(int? UserId, string? Error)> CreateUserAsync(string username, string email, string password, string? firstName, string? lastName);
    Task<(bool Success, string? Error)> AssignSingleRoleAsync(int userId, string role);
    Task<(bool Success, string? Error)> UpdateExistingUserAsync(int userId, string email, string? firstName, string? lastName, DateTime? dateOfBirth = null);
    Task<(bool Success, string? Error)> UpdatePictureAsync(int userId, byte[] picture);
    Task<(byte[]? Data, string? ContentType)> GetPictureAsync(int userId);
    Task<(bool Success, string? Error)> DeletePictureAsync(int userId);
    Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<bool> IsAddressInUseAsync(int addressId);
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\Services\IUserIdentityService.cs`
**Hash**: `803f5065e217` | **Size**: 364 chars

**Classes**: 
**Interfaces**: IUserIdentityService
```cs
using eNote.Application.Features.Identity.Users;

namespace eNote.Application.Features.Identity.Users.Services;

public interface IUserIdentityService
{
    Task<UserIdentityDto?> GetUserAsync(int userId);
    Task<IReadOnlyDictionary<int, UserIdentityDto>> GetUsersBulkAsync(IEnumerable<int> userIds);
    Task<IReadOnlyList<string>> GetRolesAsync(int userId);
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\Services\IUserProfileLookup.cs`
**Hash**: `460cbe76208b` | **Size**: 298 chars

**Classes**: 
**Interfaces**: IUserProfileLookup
### Key Cross-Cutting Interactions
- Uses **IUserProfileLookup** → User profile lookup

```cs
using eNote.Domain.Entities;

namespace eNote.Application.Features.Identity.Users.Services;

public interface IUserProfileLookup
{
    Task<Student> GetStudentAsync(int userId);
    Task<Instructor> GetInstructorAsync(int userId);
    Task<MusicStoreEmployee> GetActiveEmployeeAsync(int userId);
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\Services\IUserProfileService.cs`
**Hash**: `a5432d62990e` | **Size**: 265 chars

**Classes**: 
**Interfaces**: IUserProfileService
```cs
using eNote.Application.Features.Identity.Users;

namespace eNote.Application.Features.Identity.Users.Services;

public interface IUserProfileService
{
    Task<UserProfileResponse?> GetCurrentUserAsync();
    Task<UserProfileResponse?> GetUserAsync(int userId);
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\Services\IUserProvisioningService.cs`
**Hash**: `362f88981525` | **Size**: 475 chars

**Classes**: 
**Interfaces**: IUserProvisioningService
```cs
using eNote.Application.Features.Identity.Auth;
using eNote.Application.Features.Identity.Users;

namespace eNote.Application.Features.Identity.Users.Services;

public interface IUserProvisioningService
{
    Task<(UserProfileResponse? Profile, string? Error)> RegisterStudentAsync(RegisterRequest request);
    Task<(int UserId, string? Error)> ProvisionUserAsync(UserProvisionRequest request);
    Task UpdateMembershipAsync(int userId, UpdateMembershipRequest request);
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\Services\IUserSelfService.cs`
**Hash**: `803aa4f0ffc2` | **Size**: 536 chars

**Classes**: 
**Interfaces**: IUserSelfService
```cs
using eNote.Application.Features.Identity.Users;

namespace eNote.Application.Features.Identity.Users.Services;

public interface IUserSelfService
{
    Task<(bool Success, string? Error)> UpdateProfileAsync(UpdateProfileRequest request);
    Task<(bool Success, string? Error)> UpdatePictureAsync(byte[] picture);
    Task<(byte[]? Data, string? ContentType)> GetPictureAsync();
    Task<(bool Success, string? Error)> DeletePictureAsync();
    Task<(bool Success, string? Error)> ChangePasswordAsync(ChangePasswordRequest request);
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\Services\StudentDisplayNameService.cs`
**Hash**: `dacd0f50875c` | **Size**: 1140 chars

**Classes**: StudentDisplayNameService
### Key Cross-Cutting Interactions
- Uses **IStudentDisplayNameService** → Student display-name formatting

```cs
using eNote.Domain.Entities;
using eNote.Application.Features.Identity.Users;

namespace eNote.Application.Features.Identity.Users.Services;

public sealed class StudentDisplayNameService(IUserIdentityService identity) : IStudentDisplayNameService
{
    public async Task<string> GetStudentDisplayNameAsync(Student student)
    {
        var user = await identity.GetUserAsync(student.AppUserId);
        return user is null ? $"Student {student.Id}" : FormatName(user);
    }

    public async Task<IReadOnlyDictionary<int, string>> GetStudentDisplayNamesAsync(IEnumerable<Student> students)
    {
        List<Student> list = [.. students];
        IReadOnlyDictionary<int, UserIdentityDto> users = await identity.GetUsersBulkAsync(list.Select(s => s.AppUserId));
        return list.ToDictionary(s => s.Id, s => users.TryGetValue(s.AppUserId, out UserIdentityDto? user) ? FormatName(user) : $"Student {s.Id}");
    }

    private static string FormatName(UserIdentityDto user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.Username : fullName;
    }
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\Services\UserProfileLookup.cs`
**Hash**: `c35a63be617a` | **Size**: 1122 chars

**Classes**: UserProfileLookup
### Key Cross-Cutting Interactions
- Uses **IUserProfileLookup** → User profile lookup
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Identity.Users.Services;

public sealed class UserProfileLookup(IAppDbContext context) : IUserProfileLookup
{
    public async Task<Student> GetStudentAsync(int userId) =>
        await context.Set<Student>().AsNoTracking().FirstOrDefaultAsync(x => x.AppUserId == userId)
        ?? throw new Common.Exceptions.BusinessException(Messages.StudentProfileNotFound);

    public async Task<Instructor> GetInstructorAsync(int userId) =>
        await context.Set<Instructor>().AsNoTracking().FirstOrDefaultAsync(x => x.AppUserId == userId)
        ?? throw new Common.Exceptions.BusinessException(Messages.InstructorProfileNotFound);

    public async Task<MusicStoreEmployee> GetActiveEmployeeAsync(int userId) =>
        await context.Set<MusicStoreEmployee>().AsNoTracking().FirstOrDefaultAsync(x => x.AppUserId == userId && x.IsActive)
        ?? throw new Common.Exceptions.BusinessException(Messages.EmployeeProfileNotFound);
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\Services\UserProfileService.cs`
**Hash**: `7861f42c6e48` | **Size**: 2793 chars

**Classes**: UserProfileService
### Key Cross-Cutting Interactions
- Uses **IUserProfileLookup** → User profile lookup
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Profiles;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Identity.Users.Services;

public sealed class UserProfileService(
    IAppDbContext context,
    IUserIdentityService identity,
    IUserProfileLookup lookup,
    ICurrentUserService currentUserService) : IUserProfileService
{
    public Task<UserProfileResponse?> GetCurrentUserAsync() => GetUserAsync(currentUserService.UserId);

    public async Task<UserProfileResponse?> GetUserAsync(int userId)
    {
        var user = await identity.GetUserAsync(userId);

        if (user == null || !user.IsActive)
        {
            return null;
        }

        var roles = await identity.GetRolesAsync(userId);

        if (roles.Count != 1)
        {
            throw new BusinessException(Messages.UserSingleRoleRequired);
        }

        var role = roles[0];

        IUserProfile profile = role switch
        {
            AppRoles.Student => await BuildStudentProfile(userId, user),
            AppRoles.Instructor => await BuildInstructorProfile(userId, user),
            AppRoles.StoreEmployee => await BuildMusicStoreProfile(userId, user),
            AppRoles.Administrator => new AdminProfile(user.FirstName, user.LastName),
            _ => throw new BusinessException(Messages.UnknownRole)
        };

        return new UserProfileResponse(role, profile);
    }

    private async Task<StudentProfile> BuildStudentProfile(int userId, UserIdentityDto user)
    {
        var student = await lookup.GetStudentAsync(userId);

        return new StudentProfile(student.Id, student.EnrollmentDate, user.FirstName, user.LastName, user.DateOfBirth, user.Address, student.MembershipPaidUntil);
    }

    private async Task<InstructorProfile> BuildInstructorProfile(int userId, UserIdentityDto user)
    {
        var instructor = await lookup.GetInstructorAsync(userId);

        return new InstructorProfile(instructor.Id, user.FirstName, user.LastName);
    }

    private async Task<MusicStoreProfile> BuildMusicStoreProfile(int userId, UserIdentityDto user)
    {
        var employee = await lookup.GetActiveEmployeeAsync(userId);

        var shop = await context.Set<MusicStore>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == employee.MusicStoreId)
            ?? throw new BusinessException(Messages.StoreNotFound);

        return new MusicStoreProfile(shop.Id, shop.StoreName, shop.BusinessHours, user.Address);
    }
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\Services\UserProvisioningService.cs`
**Hash**: `fd91a1e7fc64` | **Size**: 5397 chars

**Classes**: UserProvisioningService
### Key Cross-Cutting Interactions
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Auth;
using eNote.Application.Features.Identity.Users;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Identity.Users.Services;

public sealed class UserProvisioningService(
    IAppDbContext context,
    IUserAccountService accountService,
    IUserProfileService profileService,
    IClock clock) : IUserProvisioningService
{
    public async Task<(UserProfileResponse? Profile, string? Error)> RegisterStudentAsync(RegisterRequest request)
    {
        (int? UserId, string? Error) createResult = await accountService.CreateUserAsync(
            request.Username,
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName);

        if (createResult.UserId is null)
        {
            return (null, createResult.Error);
        }

        var userId = createResult.UserId.Value;

        (var Success, var Error) = await accountService.AssignSingleRoleAsync(userId, AppRoles.Student);

        if (!Success)
        {
            return (null, Error);
        }

        await EnsureRoleProfileAsync(userId, AppRoles.Student, musicStoreId: null);
        await context.SaveChangesAsync();

        var profile = await profileService.GetUserAsync(userId);

        return (profile, null);
    }

    public async Task<(int UserId, string? Error)> ProvisionUserAsync(UserProvisionRequest request)
    {
        var username = request.Username.Trim();
        var existingUserId = await accountService.FindUserIdByUsernameAsync(username);

        int userId;

        if (existingUserId.HasValue)
        {
            userId = existingUserId.Value;

            (bool Success, string? Error) updateResult = await accountService.UpdateExistingUserAsync(
                userId,
                request.Email,
                request.FirstName,
                request.LastName);

            if (!updateResult.Success)
            {
                return (userId, updateResult.Error);
            }
        }
        else
        {
            (int? UserId, string? Error) createResult = await accountService.CreateUserAsync(
                username,
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName);

            if (createResult.UserId is null)
            {
                return (0, createResult.Error);
            }

            userId = createResult.UserId.Value;
        }

        (var Success, var Error) = await accountService.AssignSingleRoleAsync(userId, request.Role);

        if (!Success)
        {
            return (userId, Error);
        }

        var storeId = request.MusicStoreId ?? await ResolveDefaultStoreIdAsync(request.Role);

        await EnsureRoleProfileAsync(userId, request.Role, storeId);

        await context.SaveChangesAsync();

        return (userId, null);
    }

    public async Task UpdateMembershipAsync(int userId, UpdateMembershipRequest request)
    {
        var student = await context.Set<Student>()
            .FirstOrDefaultAsync(s => s.AppUserId == userId)
            ?? throw new NotFoundException(Messages.StudentProfileNotFound);

        student.UpdateMembership(request.PaidUntil);

        await context.SaveChangesAsync();
    }

    private async Task<int?> ResolveDefaultStoreIdAsync(string role)
    {
        if (role != AppRoles.StoreEmployee)
        {
            return null;
        }

        return await context.Set<MusicStore>()
            .Select(x => (int?)x.Id).FirstOrDefaultAsync();
    }

    private async Task EnsureRoleProfileAsync(int userId, string role, int? musicStoreId)
    {
        switch (role)
        {
            case AppRoles.Student:
                if (!await context.Set<Student>().AnyAsync(x => x.AppUserId == userId))
                {
                    context.Set<Student>().Add(new Student(userId, clock.UtcNow));
                }

                break;

            case AppRoles.Instructor:
                if (!await context.Set<Instructor>().AnyAsync(x => x.AppUserId == userId))
                {
                    context.Set<Instructor>().Add(new Instructor(userId));
                }

                break;

            case AppRoles.StoreEmployee when musicStoreId.HasValue:
                {
                    var employees = await context.Set<MusicStoreEmployee>()
                        .Where(x => x.AppUserId == userId)
                        .ToListAsync();

                    if (employees.Count == 0)
                    {
                        context.Set<MusicStoreEmployee>().Add(new MusicStoreEmployee(userId, musicStoreId.Value, true));
                        break;
                    }

                    var primary = employees.FirstOrDefault(x => x.IsActive) ?? employees[0];
                    primary.IsActive = true;

                    foreach (MusicStoreEmployee? employee in employees.Where(x => x.Id != primary.Id))
                    {
                        employee.IsActive = false;
                    }

                    break;
                }
        }
    }
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\Services\UserSelfService.cs`
**Hash**: `386696105c33` | **Size**: 1215 chars

**Classes**: UserSelfService
```cs
using eNote.Application.Common.Interfaces;
using eNote.Application.Features.Identity.Users;

namespace eNote.Application.Features.Identity.Users.Services;

public sealed class UserSelfService(
    IUserAccountService accountService,
    ICurrentUserService currentUserService) : IUserSelfService
{
    public Task<(bool Success, string? Error)> UpdateProfileAsync(UpdateProfileRequest request)
        => accountService.UpdateExistingUserAsync(currentUserService.UserId, request.Email, request.FirstName, request.LastName, request.DateOfBirth);

    public Task<(bool Success, string? Error)> UpdatePictureAsync(byte[] picture)
        => accountService.UpdatePictureAsync(currentUserService.UserId, picture);

    public Task<(byte[]? Data, string? ContentType)> GetPictureAsync()
        => accountService.GetPictureAsync(currentUserService.UserId);

    public Task<(bool Success, string? Error)> DeletePictureAsync()
        => accountService.DeletePictureAsync(currentUserService.UserId);

    public Task<(bool Success, string? Error)> ChangePasswordAsync(ChangePasswordRequest request)
        => accountService.ChangePasswordAsync(currentUserService.UserId, request.CurrentPassword, request.NewPassword);
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\UpdateMembershipRequest.cs`
**Hash**: `87a28dee08d2` | **Size**: 192 chars

**Classes**: UpdateMembershipRequest
```cs
namespace eNote.Application.Features.Identity.Users;

public class UpdateMembershipRequest
{
    // Null briše članstvo (označava kao neplaćeno)
    public DateTime? PaidUntil { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\UpdateProfileRequest.cs`
**Hash**: `cdba8fb327cc` | **Size**: 351 chars

**Classes**: UpdateProfileRequest
```cs
using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Identity.Users;

public class UpdateProfileRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\UserIdentityDto.cs`
**Hash**: `78fd5ac75903` | **Size**: 433 chars

**Classes**: UserIdentityDto
```cs
namespace eNote.Application.Features.Identity.Users;

public class UserIdentityDto
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public UserAddressDto? Address { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public bool HasPicture { get; set; }

    public bool IsActive { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\UserProfileResponse.cs`
**Hash**: `a19a1e5f0bfb` | **Size**: 190 chars

```cs
using eNote.Application.Features.Identity.Users.Profiles;

namespace eNote.Application.Features.Identity.Users;

public sealed record UserProfileResponse(string Role, IUserProfile Profile);

```

---

## File: `eNote\eNote.Application\Features\Identity\Users\UserProvisionRequest.cs`
**Hash**: `2e2cee0f3a21` | **Size**: 421 chars

**Classes**: UserProvisionRequest
```cs
namespace eNote.Application.Features.Identity.Users;

public class UserProvisionRequest
{
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string Role { get; init; }

    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public int? MusicStoreId { get; init; }
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\Instruments\InstrumentAvailabilityExtensions.cs`
**Hash**: `54572bcbb9b3` | **Size**: 815 chars

**Classes**: InstrumentAvailabilityExtensions
```cs
using eNote.Domain.Entities;
using eNote.Domain.Enums;

namespace eNote.Application.Features.Rentals.Instruments;

public static class InstrumentAvailabilityExtensions
{
    public static IQueryable<Instrument> WhereHasBlockingRental(this IQueryable<Instrument> query) =>
        query.Where(x => x.InstrumentRentals.Any(r => InstrumentRentalStatusSets.Blocking.Contains(r.RentalStatus)));

    public static IQueryable<Instrument> WhereHasNoBlockingRental(this IQueryable<Instrument> query) =>
        query.Where(x => !x.InstrumentRentals.Any(r => InstrumentRentalStatusSets.Blocking.Contains(r.RentalStatus)));

    public static IQueryable<InstrumentRental> WhereBlockingStatus(this IQueryable<InstrumentRental> query) =>
        query.Where(x => InstrumentRentalStatusSets.Blocking.Contains(x.RentalStatus));
}
```

---

## File: `eNote\eNote.Application\Features\Rentals\Instruments\InstrumentCreateRequest.cs`
**Hash**: `d6378335f620` | **Size**: 336 chars

**Classes**: InstrumentCreateRequest
```cs
namespace eNote.Application.Features.Rentals.Instruments;

public class InstrumentCreateRequest
{
    public string Model { get; set; } = null!;
    public string Manufacturer { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImagePath { get; set; }

    public int InstrumentTypeId { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\Instruments\InstrumentDto.cs`
**Hash**: `473bb9e958a1` | **Size**: 463 chars

**Classes**: InstrumentDto
```cs
namespace eNote.Application.Features.Rentals.Instruments;

public class InstrumentDto
{
    public int Id { get; set; }

    public string Model { get; set; } = null!;
    public string Manufacturer { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImagePath { get; set; }
    public string InstrumentType { get; set; } = null!;
    public string MusicStore { get; set; } = null!;

    public bool IsAvailable { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\Instruments\InstrumentMappingConfig.cs`
**Hash**: `c6c45ecfab42` | **Size**: 435 chars

**Classes**: InstrumentMappingConfig
```cs
using eNote.Domain.Entities;
using Mapster;

namespace eNote.Application.Features.Rentals.Instruments;

public sealed class InstrumentMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Instrument, InstrumentDto>()
            .Map(dest => dest.InstrumentType, src => src.InstrumentType.Type)
            .Map(dest => dest.MusicStore, src => src.MusicStore.StoreName);
    }
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\Instruments\InstrumentQueryableExtensions.cs`
**Hash**: `c6ed86ddd03d` | **Size**: 426 chars

**Classes**: InstrumentQueryableExtensions
```cs
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Rentals.Instruments;

public static class InstrumentQueryableExtensions
{
    public static IQueryable<Instrument> WithInstrumentDetails(this IQueryable<Instrument> query) =>
        query
            .Include(x => x.MusicStore)
            .Include(x => x.InstrumentType)
            .Include(x => x.InstrumentRentals);
}
```

---

## File: `eNote\eNote.Application\Features\Rentals\Instruments\InstrumentSearchExtensions.cs`
**Hash**: `158dea7859e3` | **Size**: 849 chars

**Classes**: InstrumentSearchExtensions
```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Search;

namespace eNote.Application.Features.Rentals.Instruments;

public static class InstrumentSearchExtensions
{
    public static IQueryable<Instrument> ApplySearch(this IQueryable<Instrument> query, InstrumentSearchObject search)
    {
        query = query
            .WhereContainsIf(search.Model, x => x.Model.Contains(search.Model!))
            .WhereContainsIf(search.Manufacturer, x => x.Manufacturer.Contains(search.Manufacturer!))
            .WhereEqualsIf(search.InstrumentTypeId, x => x.InstrumentTypeId == search.InstrumentTypeId!.Value);

        if (!search.IsAvailable.HasValue)
        {
            return query;
        }

        return search.IsAvailable.Value
            ? query.WhereHasNoBlockingRental()
            : query.WhereHasBlockingRental();
    }
}
```

---

## File: `eNote\eNote.Application\Features\Rentals\Instruments\InstrumentSearchObject.cs`
**Hash**: `50e7051d7070` | **Size**: 334 chars

**Classes**: InstrumentSearchObject
```cs
using eNote.Application.Common.Search;

namespace eNote.Application.Features.Rentals.Instruments;

public class InstrumentSearchObject : BaseSearchObject
{
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public int? InstrumentTypeId { get; set; }

    public bool? IsAvailable { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\Instruments\InstrumentUpdateRequest.cs`
**Hash**: `6bb1d5655b64` | **Size**: 321 chars

**Classes**: InstrumentUpdateRequest
```cs
namespace eNote.Application.Features.Rentals.Instruments;

public class InstrumentUpdateRequest
{
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public string? Description { get; set; }
    public string? ImagePath { get; set; }

    public int? InstrumentTypeId { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\Instruments\Services\IInstrumentService.cs`
**Hash**: `a3cccd1a3d6d` | **Size**: 784 chars

**Classes**: 
**Interfaces**: IInstrumentService
```cs
using eNote.Application.Common.Paging;
using eNote.Application.Features.Rentals.Instruments;

namespace eNote.Application.Features.Rentals.Instruments.Services;

public interface IInstrumentService
{
    Task<InstrumentDto> GetByIdAsync(int id);
    Task<InstrumentDto> GetPublicByIdAsync(int id);
    Task<PagedResult<InstrumentDto>> GetPagedAsync(InstrumentSearchObject search);
    Task<PagedResult<InstrumentDto>> GetPublicPagedAsync(InstrumentSearchObject search);
    Task<InstrumentDto> CreateAsync(InstrumentCreateRequest request);
    Task<InstrumentDto> UpdateAsync(int id, InstrumentUpdateRequest request);
    Task DeleteAsync(int id);
    Task<InstrumentDto> UploadImageAsync(int id, Stream stream, string fileName, string contentType, CancellationToken ct = default);
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\Instruments\Services\InstrumentService.cs`
**Hash**: `d656f1ffaae1` | **Size**: 5722 chars

**Classes**: InstrumentService
### Key Cross-Cutting Interactions
- Uses **ICurrentActor** → Current actor resolution
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Rentals.Instruments;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Rentals.Instruments.Services;

public sealed class InstrumentService(
    IAppDbContext context,
    IMapper mapper,
    ICurrentActor actor,
    IFileStorageService fileStorage) : IInstrumentService
{
    public async Task<InstrumentDto> GetByIdAsync(int id)
    {
        var employee = await EnsureStoreAccessAsync();

        var entity = await context.Set<Instrument>()
            .AsNoTracking()
            .WithInstrumentDetails()
            .FirstOrDefaultAsync(x => x.Id == id && x.MusicStoreId == employee.MusicStoreId)
            ?? throw new NotFoundException(Messages.NotFound);

        return mapper.Map<InstrumentDto>(entity);
    }

    public async Task<InstrumentDto> GetPublicByIdAsync(int id)
    {
        var entity = await context.Set<Instrument>()
            .AsNoTracking()
            .WithInstrumentDetails()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException(Messages.NotFound);

        return mapper.Map<InstrumentDto>(entity);
    }

    public async Task<PagedResult<InstrumentDto>> GetPagedAsync(InstrumentSearchObject search)
    {
        var employee = await EnsureStoreAccessAsync();

        var query = context.Set<Instrument>()
            .AsNoTracking()
            .Where(x => x.MusicStoreId == employee.MusicStoreId)
            .WithInstrumentDetails()
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<InstrumentDto>);
    }

    public async Task<PagedResult<InstrumentDto>> GetPublicPagedAsync(InstrumentSearchObject search)
    {
        var query = context.Set<Instrument>()
            .AsNoTracking()
            .WithInstrumentDetails()
            .ApplySearch(search);

        return await query.ToPagedResultAsync(search, mapper.Map<InstrumentDto>);
    }

    public async Task<InstrumentDto> CreateAsync(InstrumentCreateRequest request)
    {
        var employee = await EnsureStoreAccessAsync();
        await EnsureInstrumentTypeExistsAsync(request.InstrumentTypeId);

        var entity = new Instrument(
            request.Model.Trim(),
            request.Manufacturer.Trim(),
            request.Description?.Trim(),
            request.ImagePath?.Trim(),
            request.InstrumentTypeId,
            employee.MusicStoreId);

        context.Set<Instrument>().Add(entity);
        await context.SaveChangesAsync();

        return mapper.Map<InstrumentDto>(await ReloadAsync(entity.Id));
    }

    public async Task<InstrumentDto> UpdateAsync(int id, InstrumentUpdateRequest request)
    {
        var employee = await EnsureStoreAccessAsync();

        var entity = await context.Set<Instrument>()
            .FirstOrDefaultAsync(x => x.Id == id && x.MusicStoreId == employee.MusicStoreId)
            ?? throw new NotFoundException(Messages.NotFound);

        if (request.InstrumentTypeId is int typeId)
        {
            await EnsureInstrumentTypeExistsAsync(typeId);
        }

        entity.UpdateDetails(
            request.Model?.Trim() ?? entity.Model,
            request.Manufacturer?.Trim() ?? entity.Manufacturer,
            request.Description?.Trim() ?? entity.Description,
            request.ImagePath?.Trim() ?? entity.ImagePath,
            request.InstrumentTypeId ?? entity.InstrumentTypeId);

        await context.SaveChangesAsync();

        return mapper.Map<InstrumentDto>(await ReloadAsync(entity.Id));
    }

    public async Task<InstrumentDto> UploadImageAsync(int id, Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        var employee = await EnsureStoreAccessAsync();

        var entity = await context.Set<Instrument>()
            .FirstOrDefaultAsync(x => x.Id == id && x.MusicStoreId == employee.MusicStoreId, ct)
            ?? throw new NotFoundException(Messages.InstrumentNotFound);

        var path = await fileStorage.SaveAsync(stream, fileName, contentType, "instruments", ct);
        entity.UpdateDetails(entity.Model, entity.Manufacturer, entity.Description, path, entity.InstrumentTypeId);

        await context.SaveChangesAsync(ct);

        return mapper.Map<InstrumentDto>(await ReloadAsync(entity.Id));
    }

    public async Task DeleteAsync(int id)
    {
        var employee = await EnsureStoreAccessAsync();

        var instrument = await context.Set<Instrument>()
            .FirstOrDefaultAsync(x => x.Id == id && x.MusicStoreId == employee.MusicStoreId)
            ?? throw new NotFoundException(Messages.NotFound);

        if (await context.Set<InstrumentRental>().WhereBlockingStatus().AnyAsync(r => r.InstrumentId == id))
        {
            throw new BusinessException(Messages.InstrumentDeleteBlocked);
        }

        instrument.SoftDelete();
        await context.SaveChangesAsync();
    }

    private Task<MusicStoreEmployee> EnsureStoreAccessAsync() =>
        actor.GetCurrentEmployeeAsync();

    private async Task EnsureInstrumentTypeExistsAsync(int instrumentTypeId)
    {
        if (!await context.Set<InstrumentType>().AnyAsync(x => x.Id == instrumentTypeId))
        {
            throw new BusinessException(Messages.InstrumentTypeNotFound);
        }
    }

    private Task<Instrument> ReloadAsync(int id) =>
        context.Set<Instrument>().AsNoTracking().WithInstrumentDetails().FirstAsync(x => x.Id == id);
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\Recommendations\InstrumentRecommendationDto.cs`
**Hash**: `4f443b312d54` | **Size**: 322 chars

**Classes**: InstrumentRecommendationDto
```cs
using eNote.Application.Features.Rentals.Instruments;

namespace eNote.Application.Features.Rentals.Recommendations;

public class InstrumentRecommendationDto
{
    public InstrumentDto Instrument { get; set; } = null!;
    public double Score { get; set; }
    public IReadOnlyList<string> Reasons { get; init; } = [];
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\Recommendations\Services\IRecommendationService.cs`
**Hash**: `24e963912e04` | **Size**: 423 chars

**Classes**: 
**Interfaces**: IRecommendationService
```cs
using eNote.Application.Features.Rentals.Recommendations;

namespace eNote.Application.Features.Rentals.Recommendations.Services;

public interface IRecommendationService
{
    Task<IReadOnlyList<InstrumentRecommendationDto>> GetRecommendedInstrumentsAsync(int count = 5, CancellationToken cancellationToken = default);
    Task RecordInstrumentViewAsync(int instrumentId, CancellationToken cancellationToken = default);
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\Recommendations\Services\RecommendationService.cs`
**Hash**: `fdf2e20f3b6f` | **Size**: 12199 chars

**Classes**: RecommendationService
### Key Cross-Cutting Interactions
- Uses **ICurrentActor** → Current actor resolution
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Rentals.Instruments;
using eNote.Application.Features.Rentals.Recommendations;
using eNote.Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Rentals.Recommendations.Services;

public sealed class RecommendationService(IAppDbContext context, IMapper mapper, ICurrentActor actor, IClock clock) : IRecommendationService
{
    private const double RentalWeight = 0.40;
    private const double ViewWeight = 0.30;
    private const double SimilarityWeight = 0.20;
    private const double PopularityWeight = 0.10;
    private const int CandidatePoolSize = 80;

    public async Task<IReadOnlyList<InstrumentRecommendationDto>> GetRecommendedInstrumentsAsync(int count = 5, CancellationToken cancellationToken = default)
    {
        count = NormalizeCount(count);

        var studentId = await actor.GetCurrentStudentIdAsync();

        var userId = actor.UserId;

        var userRentals = await context.Set<InstrumentRental>()
            .AsNoTracking()
            .Where(x => x.StudentProfileId == studentId && InstrumentRentalStatusSets.History.Contains(x.RentalStatus))
            .Select(x => new UserRentalSnapshot(x.InstrumentId, x.Instrument.InstrumentTypeId, x.Instrument.Manufacturer))
            .ToListAsync(cancellationToken);

        HashSet<int> rentedInstrumentIds = [.. userRentals.Select(x => x.InstrumentId)];

        Dictionary<int, InstrumentViewSnapshot> viewMap = await context.Set<InstrumentView>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToDictionaryAsync(x => x.InstrumentId, x => new InstrumentViewSnapshot(x.ViewCount, x.LastViewedAt), cancellationToken);

        Dictionary<int, int> globalRentalCounts = await context.Set<InstrumentRental>()
            .AsNoTracking()
            .Where(x => InstrumentRentalStatusSets.History.Contains(x.RentalStatus))
            .GroupBy(x => x.InstrumentId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        var maxGlobalRentals = globalRentalCounts.Values.DefaultIfEmpty(0).Max();

        var maxUserViews = viewMap.Values.Select(x => x.ViewCount).DefaultIfEmpty(0).Max();

        var userTypeCounts = userRentals
            .GroupBy(x => x.InstrumentTypeId)
            .ToDictionary(g => g.Key, g => g.Count());

        var preferredTypeId = userTypeCounts
            .OrderByDescending(x => x.Value)
            .Select(x => x.Key)
            .Cast<int?>()
            .FirstOrDefault();

        var preferredManufacturer = userRentals
            .GroupBy(x => x.Manufacturer)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();

        var collaborativeInstrumentIds = await BuildCollaborativeInstrumentIdsAsync(studentId, rentedInstrumentIds, cancellationToken);

        var preferredTypeIds = userTypeCounts.Keys.ToList();

        var candidates = await LoadCandidateInstrumentsAsync(
            preferredTypeIds, collaborativeInstrumentIds, count, cancellationToken);

        if (candidates.Count == 0)
        {
            return [];
        }

        List<ScoredRecommendation> scored = [];

        foreach (Instrument instrument in candidates)
        {
            var rentalScore = ComputeRentalScore(instrument, userTypeCounts, collaborativeInstrumentIds);
            var viewScore = ComputeViewScore(instrument, viewMap, maxUserViews, userTypeCounts);

            var similarityScore = ComputeSimilarityScore(instrument, preferredTypeId, preferredManufacturer);
            var popularityScore = maxGlobalRentals == 0 ? 0 : (double)globalRentalCounts.GetValueOrDefault(instrument.Id) / maxGlobalRentals;
            var totalScore = rentalScore * RentalWeight + viewScore * ViewWeight + similarityScore * SimilarityWeight + popularityScore * PopularityWeight;

            var reasons = BuildReasons(rentalScore, viewScore, similarityScore, popularityScore, instrument, preferredTypeId, collaborativeInstrumentIds);

            scored.Add(new ScoredRecommendation(instrument, totalScore, reasons));
        }

        return [.. scored
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Instrument.IsAvailable)
            .ThenBy(x => x.Instrument.Id)
            .Take(count)
            .Select(x => new InstrumentRecommendationDto
            {
                Instrument = mapper.Map<InstrumentDto>(x.Instrument),
                Score = Math.Round(x.Score, 4),
                Reasons = x.Reasons
            })];
    }

    public async Task RecordInstrumentViewAsync(int instrumentId, CancellationToken cancellationToken = default)
    {
        var instrumentExists = await context.Set<Instrument>()
            .AnyAsync(x => x.Id == instrumentId && x.IsActive, cancellationToken);

        if (!instrumentExists)
        {
            throw new NotFoundException(Messages.InstrumentNotFound);
        }

        var userId = actor.UserId;

        var now = clock.UtcNow;

        var view = await context.Set<InstrumentView>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.InstrumentId == instrumentId, cancellationToken);

        if (view is null)
        {
            context.Set<InstrumentView>().Add(new InstrumentView(userId, instrumentId, now));
        }
        else
        {
            view.RecordView(now);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<Instrument>> LoadCandidateInstrumentsAsync(IReadOnlyList<int> preferredTypeIds, HashSet<int> collaborativeInstrumentIds, int count, CancellationToken cancellationToken)
    {
        var poolSize = Math.Max(count * 12, CandidatePoolSize);

        var popularIds = await context.Set<InstrumentRental>()
            .AsNoTracking()
            .Where(x => InstrumentRentalStatusSets.History.Contains(x.RentalStatus))
            .GroupBy(x => x.InstrumentId)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(poolSize / 2)
            .ToListAsync(cancellationToken);

        List<int> preferredIds = preferredTypeIds.Count == 0 ? [] : await context.Set<Instrument>()
                .AsNoTracking()
                .Where(x => x.IsActive && preferredTypeIds.Contains(x.InstrumentTypeId))
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .Take(poolSize / 2)
                .ToListAsync(cancellationToken);

        var candidateIds = preferredIds
            .Concat(collaborativeInstrumentIds)
            .Concat(popularIds)
            .Distinct()
            .Take(poolSize)
            .ToList();

        if (candidateIds.Count < count)
        {
            var fillerIds = await context.Set<Instrument>()
                .AsNoTracking()
                .Where(x => x.IsActive && !candidateIds.Contains(x.Id))
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .Take(poolSize - candidateIds.Count)
                .ToListAsync(cancellationToken);

            candidateIds.AddRange(fillerIds);
        }

        return await context.Set<Instrument>()
            .AsNoTracking()
            .WithInstrumentDetails()
            .Where(x => candidateIds.Contains(x.Id) && x.IsActive)
            .ToListAsync(cancellationToken);
    }

    private async Task<HashSet<int>> BuildCollaborativeInstrumentIdsAsync(int studentId, HashSet<int> rentedInstrumentIds, CancellationToken cancellationToken)
    {
        if (rentedInstrumentIds.Count == 0)
        {
            return [];
        }

        var similarStudentIds = await context.Set<InstrumentRental>()
            .AsNoTracking()
            .Where(x => rentedInstrumentIds.Contains(x.InstrumentId)
                && x.StudentProfileId != studentId
                && InstrumentRentalStatusSets.History.Contains(x.RentalStatus))
            .Select(x => x.StudentProfileId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (similarStudentIds.Count == 0)
        {
            return [];
        }

        var collaborativeIds = await context.Set<InstrumentRental>()
            .AsNoTracking()
            .Where(x => similarStudentIds.Contains(x.StudentProfileId) && InstrumentRentalStatusSets.History.Contains(x.RentalStatus) && !rentedInstrumentIds.Contains(x.InstrumentId))
            .Select(x => x.InstrumentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return [.. collaborativeIds];
    }

    private static double ComputeRentalScore(Instrument instrument, Dictionary<int, int> userTypeCounts, HashSet<int> collaborativeInstrumentIds)
    {
        double ownHistoryScore = 0;

        if (userTypeCounts.TryGetValue(instrument.InstrumentTypeId, out var typeCount) && typeCount > 0)
        {
            var maxTypeCount = userTypeCounts.Values.Max();

            ownHistoryScore = (double)typeCount / maxTypeCount;
        }

        var collaborativeScore = collaborativeInstrumentIds.Contains(instrument.Id) ? 1 : 0;

        if (ownHistoryScore > 0 && collaborativeScore > 0)
        {
            return ownHistoryScore * 0.6 + collaborativeScore * 0.4;
        }

        return Math.Max(ownHistoryScore, collaborativeScore);
    }

    private static double ComputeViewScore(Instrument instrument, Dictionary<int, InstrumentViewSnapshot> viewMap, int maxUserViews, Dictionary<int, int> userTypeCounts)
    {
        if (viewMap.TryGetValue(instrument.Id, out var directView) && maxUserViews > 0)
        {
            return (double)directView.ViewCount / maxUserViews;
        }

        if (userTypeCounts.ContainsKey(instrument.InstrumentTypeId))
        {
            return 0.35;
        }

        return 0;
    }

    private static double ComputeSimilarityScore(Instrument instrument, int? preferredTypeId, string? preferredManufacturer)
    {
        if (preferredTypeId is null)
        {
            return 0;
        }

        if (string.Equals(instrument.Manufacturer, preferredManufacturer, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (instrument.InstrumentTypeId == preferredTypeId)
        {
            return 0.6;
        }

        return 0;
    }

    private static List<string> BuildReasons(double rentalScore, double viewScore, double similarityScore, double popularityScore, Instrument instrument, int? preferredTypeId, HashSet<int> collaborativeInstrumentIds)
    {
        List<string> reasons = [];

        if (rentalScore >= 0.5 && preferredTypeId == instrument.InstrumentTypeId)
        {
            reasons.Add($"Na osnovu vaše historije najma ({instrument.InstrumentType.Type}).");
        }

        if (collaborativeInstrumentIds.Contains(instrument.Id))
        {
            reasons.Add("Studenti sa sličnim izborima najma biraju ovaj instrument.");
        }

        if (viewScore >= 0.5)
        {
            reasons.Add("Pregledali ste ovaj instrument ili slične modele.");
        }

        if (similarityScore >= 0.6)
        {
            reasons.Add("Sličan vašim prethodnim izborima proizvođača ili vrste.");
        }

        if (popularityScore >= 0.5)
        {
            reasons.Add("Popularan među studentima.");
        }

        if (reasons.Count == 0)
        {
            reasons.Add("Preporučeno na osnovu dostupnosti i ukupnog interesovanja.");
        }

        return reasons;
    }

    private static int NormalizeCount(int count) => count < 1 ? 1 : count > 20 ? 20 : count;

    private sealed record UserRentalSnapshot(int InstrumentId, int InstrumentTypeId, string Manufacturer);

    private sealed record InstrumentViewSnapshot(int ViewCount, DateTime LastViewedAt);

    private sealed record ScoredRecommendation(Instrument Instrument, double Score, List<string> Reasons);
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\ReferenceData\Addresses\AddressReferenceDto.cs`
**Hash**: `6f6ee229d091` | **Size**: 293 chars

**Classes**: AddressReferenceDto
```cs
namespace eNote.Application.Features.Rentals.ReferenceData.Addresses;

public sealed class AddressReferenceDto
{
    public int Id { get; init; }
    public string City { get; init; } = null!;
    public string Street { get; init; } = null!;
    public string Number { get; init; } = null!;
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\ReferenceData\Addresses\AddressRequest.cs`
**Hash**: `0e93352fceb6` | **Size**: 252 chars

**Classes**: AddressRequest
```cs
namespace eNote.Application.Features.Rentals.ReferenceData.Addresses;

public sealed class AddressRequest
{
    public string City { get; set; } = null!;
    public string Street { get; set; } = null!;
    public string Number { get; set; } = null!;
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\ReferenceData\Addresses\AddressSearchExtensions.cs`
**Hash**: `f5a164a608f7` | **Size**: 469 chars

**Classes**: AddressSearchExtensions
```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Search;

namespace eNote.Application.Features.Rentals.ReferenceData.Addresses;

public static class AddressSearchExtensions
{
    public static IQueryable<Address> ApplySearch(this IQueryable<Address> query, AddressSearchObject search) => query
            .WhereContainsIf(search.City, x => x.City.Contains(search.City!))
            .WhereContainsIf(search.Street, x => x.Street.Contains(search.Street!));
}
```

---

## File: `eNote\eNote.Application\Features\Rentals\ReferenceData\Addresses\AddressSearchObject.cs`
**Hash**: `ccc274b15f88` | **Size**: 251 chars

**Classes**: AddressSearchObject
```cs
using eNote.Application.Common.Search;

namespace eNote.Application.Features.Rentals.ReferenceData.Addresses;

public sealed class AddressSearchObject : BaseSearchObject
{
    public string? City { get; set; }
    public string? Street { get; set; }
}
```

---

## File: `eNote\eNote.Application\Features\Rentals\ReferenceData\Addresses\AddressService.cs`
**Hash**: `f3cf6a81c159` | **Size**: 1860 chars

**Classes**: AddressService
### Key Cross-Cutting Interactions
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Rentals.ReferenceData;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Rentals.ReferenceData.Addresses;

public sealed class AddressService(IAppDbContext context, IUserAccountService accountService) : ReferenceCrudService<Address, AddressReferenceDto, AddressRequest, AddressSearchObject>(context), IAddressService
{
    protected override string NotFoundMessage => Messages.AddressNotFound;

    protected override AddressReferenceDto Map(Address entity) => new()
    {
        Id = entity.Id,
        City = entity.City,
        Street = entity.Street,
        Number = entity.Number
    };

    protected override Address CreateEntity(AddressRequest request) => new()
    {
        City = request.City.Trim(),
        Street = request.Street.Trim(),
        Number = request.Number.Trim()
    };

    protected override void ApplyUpdate(Address entity, AddressRequest request)
    {
        entity.City = request.City.Trim();
        entity.Street = request.Street.Trim();
        entity.Number = request.Number.Trim();
    }

    protected override IQueryable<Address> ApplySearch(IQueryable<Address> query, AddressSearchObject search) => query.ApplySearch(search);
    protected override IOrderedQueryable<Address> Order(IQueryable<Address> query) => query.OrderBy(x => x.City).ThenBy(x => x.Street);

    protected override async Task EnsureDeletableAsync(Address entity, CancellationToken ct = default)
    {
        if (await accountService.IsAddressInUseAsync(entity.Id))
        {
            throw new BusinessException(Messages.AddressDeleteBlocked);
        }
    }
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\ReferenceData\Addresses\IAddressService.cs`
**Hash**: `b2a3a0272d4b` | **Size**: 247 chars

**Classes**: 
**Interfaces**: IAddressService
```cs
using eNote.Application.Features.Rentals.ReferenceData;

namespace eNote.Application.Features.Rentals.ReferenceData.Addresses;

public interface IAddressService : IReferenceCrudService<AddressReferenceDto, AddressRequest, AddressSearchObject>
{
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\ReferenceData\IReferenceCrudService.cs`
**Hash**: `ff09f19641b6` | **Size**: 467 chars

**Classes**: 
**Interfaces**: IReferenceCrudService
```cs
using eNote.Application.Common.Paging;
using eNote.Application.Common.Search;

namespace eNote.Application.Features.Rentals.ReferenceData;

public interface IReferenceCrudService<TDto, TRequest, TSearch> where TSearch : BaseSearchObject
{
    Task<PagedResult<TDto>> GetPagedAsync(TSearch search);
    Task<TDto> GetByIdAsync(int id);
    Task<TDto> CreateAsync(TRequest request);
    Task<TDto> UpdateAsync(int id, TRequest request);
    Task DeleteAsync(int id);
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\ReferenceData\InstrumentTypes\IInstrumentTypeService.cs`
**Hash**: `087749396844` | **Size**: 272 chars

**Classes**: 
**Interfaces**: IInstrumentTypeService
```cs
using eNote.Application.Features.Rentals.ReferenceData;

namespace eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;

public interface IInstrumentTypeService : IReferenceCrudService<InstrumentTypeDto, InstrumentTypeRequest, InstrumentTypeSearchObject>
{
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\ReferenceData\InstrumentTypes\InstrumentTypeDto.cs`
**Hash**: `57b0cddb8d01` | **Size**: 244 chars

**Classes**: InstrumentTypeDto
```cs
namespace eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;

public sealed class InstrumentTypeDto
{
    public int Id { get; init; }
    public string Type { get; init; } = null!;
    public decimal MonthlyFee { get; init; }
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\ReferenceData\InstrumentTypes\InstrumentTypeRequest.cs`
**Hash**: `b9633feb6e24` | **Size**: 213 chars

**Classes**: InstrumentTypeRequest
```cs
namespace eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;

public sealed class InstrumentTypeRequest
{
    public string Type { get; set; } = null!;
    public decimal MonthlyFee { get; set; }
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\ReferenceData\InstrumentTypes\InstrumentTypeSearchExtensions.cs`
**Hash**: `c0ef050174ea` | **Size**: 406 chars

**Classes**: InstrumentTypeSearchExtensions
```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Search;

namespace eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;

public static class InstrumentTypeSearchExtensions
{
    public static IQueryable<InstrumentType> ApplySearch(this IQueryable<InstrumentType> query, InstrumentTypeSearchObject search) => query.WhereContainsIf(search.Type, x => x.Type.Contains(search.Type!));
}
```

---

## File: `eNote\eNote.Application\Features\Rentals\ReferenceData\InstrumentTypes\InstrumentTypeSearchObject.cs`
**Hash**: `8ee564078e61` | **Size**: 224 chars

**Classes**: InstrumentTypeSearchObject
```cs
using eNote.Application.Common.Search;

namespace eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;

public sealed class InstrumentTypeSearchObject : BaseSearchObject
{
    public string? Type { get; set; }
}
```

---

## File: `eNote\eNote.Application\Features\Rentals\ReferenceData\InstrumentTypes\InstrumentTypeService.cs`
**Hash**: `60247f84d98d` | **Size**: 1792 chars

**Classes**: InstrumentTypeService
### Key Cross-Cutting Interactions
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Rentals.ReferenceData;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;

public sealed class InstrumentTypeService(IAppDbContext context)
    : ReferenceCrudService<InstrumentType, InstrumentTypeDto, InstrumentTypeRequest, InstrumentTypeSearchObject>(context), IInstrumentTypeService
{
    protected override string NotFoundMessage => Messages.InstrumentTypeNotFound;

    protected override InstrumentTypeDto Map(InstrumentType entity) => new()
    {
        Id = entity.Id,
        Type = entity.Type,
        MonthlyFee = entity.MonthlyFee
    };

    protected override InstrumentType CreateEntity(InstrumentTypeRequest request) => new()
    {
        Type = request.Type.Trim(),
        MonthlyFee = request.MonthlyFee
    };

    protected override void ApplyUpdate(InstrumentType entity, InstrumentTypeRequest request)
    {
        entity.Type = request.Type.Trim();
        entity.MonthlyFee = request.MonthlyFee;
    }

    protected override IQueryable<InstrumentType> ApplySearch(IQueryable<InstrumentType> query, InstrumentTypeSearchObject search) => query.ApplySearch(search);
    protected override IOrderedQueryable<InstrumentType> Order(IQueryable<InstrumentType> query) => query.OrderBy(x => x.Type);

    protected override async Task EnsureDeletableAsync(InstrumentType entity, CancellationToken ct = default)
    {
        if (await Db.Set<Instrument>().AnyAsync(x => x.InstrumentTypeId == entity.Id, ct))
        {
            throw new BusinessException(Messages.InstrumentTypeDeleteBlocked);
        }
    }
}
```

---

## File: `eNote\eNote.Application\Features\Rentals\ReferenceData\MusicStores\IMusicStoreService.cs`
**Hash**: `a45d255df6e9` | **Size**: 252 chars

**Classes**: 
**Interfaces**: IMusicStoreService
```cs
using eNote.Application.Features.Rentals.ReferenceData;

namespace eNote.Application.Features.Rentals.ReferenceData.MusicStores;

public interface IMusicStoreService : IReferenceCrudService<MusicStoreDto, MusicStoreRequest, MusicStoreSearchObject>
{
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\ReferenceData\MusicStores\MusicStoreDto.cs`
**Hash**: `1264e2717217` | **Size**: 252 chars

**Classes**: MusicStoreDto
```cs
namespace eNote.Application.Features.Rentals.ReferenceData.MusicStores;

public sealed class MusicStoreDto
{
    public int Id { get; init; }
    public string StoreName { get; init; } = null!;
    public string BusinessHours { get; init; } = null!;
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\ReferenceData\MusicStores\MusicStoreRequest.cs`
**Hash**: `acef42be64ac` | **Size**: 221 chars

**Classes**: MusicStoreRequest
```cs
namespace eNote.Application.Features.Rentals.ReferenceData.MusicStores;

public sealed class MusicStoreRequest
{
    public string StoreName { get; set; } = null!;
    public string BusinessHours { get; set; } = null!;
}

```

---

## File: `eNote\eNote.Application\Features\Rentals\ReferenceData\MusicStores\MusicStoreSearchExtensions.cs`
**Hash**: `db745fb3178d` | **Size**: 401 chars

**Classes**: MusicStoreSearchExtensions
```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Search;

namespace eNote.Application.Features.Rentals.ReferenceData.MusicStores;

public static class MusicStoreSearchExtensions
{
    public static IQueryable<MusicStore> ApplySearch(this IQueryable<MusicStore> query, MusicStoreSearchObject search) => query.WhereContainsIf(search.StoreName, x => x.StoreName.Contains(search.StoreName!));
}
```

---

## File: `eNote\eNote.Application\Features\Rentals\ReferenceData\MusicStores\MusicStoreSearchObject.cs`
**Hash**: `c540c47aa47f` | **Size**: 221 chars

**Classes**: MusicStoreSearchObject
```cs
using eNote.Application.Common.Search;

namespace eNote.Application.Features.Rentals.ReferenceData.MusicStores;

public sealed class MusicStoreSearchObject : BaseSearchObject
{
    public string? StoreName { get; set; }
}
```

---

## File: `eNote\eNote.Application\Features\Rentals\ReferenceData\MusicStores\MusicStoreService.cs`
**Hash**: `e4e465a592c0` | **Size**: 1784 chars

**Classes**: MusicStoreService
### Key Cross-Cutting Interactions
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Rentals.ReferenceData;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Rentals.ReferenceData.MusicStores;

public sealed class MusicStoreService(IAppDbContext context) : ReferenceCrudService<MusicStore, MusicStoreDto, MusicStoreRequest, MusicStoreSearchObject>(context), IMusicStoreService
{
    protected override string NotFoundMessage => Messages.StoreNotFound;

    protected override MusicStoreDto Map(MusicStore entity) => new()
    {
        Id = entity.Id,
        StoreName = entity.StoreName,
        BusinessHours = entity.BusinessHours
    };

    protected override MusicStore CreateEntity(MusicStoreRequest request) => new(request.StoreName.Trim(), request.BusinessHours.Trim());

    protected override void ApplyUpdate(MusicStore entity, MusicStoreRequest request) => entity.UpdateDetails(request.StoreName.Trim(), request.BusinessHours.Trim());
    protected override IQueryable<MusicStore> ApplySearch(IQueryable<MusicStore> query, MusicStoreSearchObject search) => query.ApplySearch(search);
    protected override IOrderedQueryable<MusicStore> Order(IQueryable<MusicStore> query) => query.OrderBy(x => x.StoreName);

    protected override async Task EnsureDeletableAsync(MusicStore entity, CancellationToken ct = default)
    {
        var inUse = await Db.Set<Instrument>().AnyAsync(x => x.MusicStoreId == entity.Id, ct)
            || await Db.Set<MusicStoreEmployee>().AnyAsync(x => x.MusicStoreId == entity.Id, ct);

        if (inUse)
        {
            throw new BusinessException(Messages.MusicStoreDeleteBlocked);
        }
    }
}
```

---

## File: `eNote\eNote.Application\Features\Rentals\ReferenceData\ReferenceCrudService.cs`
**Hash**: `8ad6c1c66ed1` | **Size**: 2456 chars

**Classes**: ReferenceCrudService
### Key Cross-Cutting Interactions
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Search;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Rentals.ReferenceData;

public abstract class ReferenceCrudService<TEntity, TDto, TRequest, TSearch>(IAppDbContext context)
    : IReferenceCrudService<TDto, TRequest, TSearch>
    where TEntity : BaseEntity
    where TSearch : BaseSearchObject
{
    protected IAppDbContext Db => context;

    protected abstract string NotFoundMessage { get; }
    protected abstract TDto Map(TEntity entity);
    protected abstract TEntity CreateEntity(TRequest request);
    protected abstract void ApplyUpdate(TEntity entity, TRequest request);
    protected abstract IOrderedQueryable<TEntity> Order(IQueryable<TEntity> query);
    protected abstract IQueryable<TEntity> ApplySearch(IQueryable<TEntity> query, TSearch search);
    protected virtual Task EnsureDeletableAsync(TEntity entity, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<PagedResult<TDto>> GetPagedAsync(TSearch search) =>
        Order(ApplySearch(Db.Set<TEntity>().AsNoTracking(), search)).ToPagedResultAsync(search, Map);

    public async Task<TDto> GetByIdAsync(int id)
    {
        var entity = await Db.Set<TEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException(NotFoundMessage);

        return Map(entity);
    }

    public async Task<TDto> CreateAsync(TRequest request)
    {
        var entity = CreateEntity(request);

        Db.Set<TEntity>().Add(entity);
        await Db.SaveChangesAsync();

        return Map(entity);
    }

    public async Task<TDto> UpdateAsync(int id, TRequest request)
    {
        var entity = await Db.Set<TEntity>()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException(NotFoundMessage);

        ApplyUpdate(entity, request);
        await Db.SaveChangesAsync();

        return Map(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await Db.Set<TEntity>()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException(NotFoundMessage);

        await EnsureDeletableAsync(entity);
        Db.Set<TEntity>().Remove(entity);

        await Db.SaveChangesAsync();
    }
}
```

---

## File: `eNote\eNote.Application\Features\Reports\Services\IReportService.cs`
**Hash**: `0c14af9ba66d` | **Size**: 414 chars

**Classes**: 
**Interfaces**: IReportService
```cs
namespace eNote.Application.Features.Reports.Services;

public interface IReportService
{
    Task<byte[]> GenerateCourseRankingPdfAsync(int courseId, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateStoreRentalSummaryPdfAsync(CancellationToken cancellationToken = default);
    Task<byte[]> GenerateLectureAttendancePdfAsync(int lectureId, CancellationToken cancellationToken = default);
}
```

---

## File: `eNote\eNote.Application\Features\Reports\Services\ReportService.cs`
**Hash**: `83b56a5c32ba` | **Size**: 8506 chars

**Classes**: ReportService
### Key Cross-Cutting Interactions
- Uses **IInstructorAccessService** → Instructor ownership enforcement
- Uses **ICurrentActor** → Current actor resolution
- Uses **IStudentDisplayNameService** → Student display-name formatting
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Domain.Entities;
using System.Globalization;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Rentals.InstrumentRentals.Billing;
using eNote.Application.Features.Identity.Instructors;

namespace eNote.Application.Features.Reports.Services;

public sealed class ReportService(IAppDbContext context, IClock clock, IRankingService rankingService, IInstructorAccessService instructorAccess, ICurrentActor actor, IStudentDisplayNameService displayNames) : IReportService
{
    private static readonly CultureInfo ReportCulture = CultureInfo.GetCultureInfo("bs-BA");

    static ReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateCourseRankingPdfAsync(int courseId, CancellationToken cancellationToken = default)
    {
        var entries = await rankingService.GetForInstructorAsync(courseId);

        var courseName = await context.Set<Course>()
            .AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? $"Kurs {courseId}";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Text($"Rang lista — {courseName}").Bold().FontSize(18);
                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Rang");
                        header.Cell().Element(CellStyle).Text("Student");
                        header.Cell().Element(CellStyle).Text("Prosjek");
                        header.Cell().Element(CellStyle).Text("Ocijenjeno");
                    });

                    foreach (var entry in entries)
                    {
                        table.Cell().Element(CellStyle).Text(entry.Rank.ToString());
                        table.Cell().Element(CellStyle).Text(entry.StudentName);
                        table.Cell().Element(CellStyle).Text(entry.AverageGrade?.ToString("F2", ReportCulture) ?? "-");
                        table.Cell().Element(CellStyle).Text(entry.GradedSubmissions.ToString());
                    }
                });
                page.Footer().AlignRight().Text($"Generisano: {clock.UtcNow:dd.MM.yyyy HH:mm} UTC").FontSize(9);
            });
        }).GeneratePdf();
    }

    public async Task<byte[]> GenerateStoreRentalSummaryPdfAsync(CancellationToken cancellationToken = default)
    {
        var storeId = await actor.GetCurrentStoreIdAsync();

        var storeName = await context.Set<MusicStore>()
            .AsNoTracking()
            .Where(s => s.Id == storeId)
            .Select(s => s.StoreName)
            .FirstOrDefaultAsync(cancellationToken) ?? $"Prodavnica {storeId}";

        var rentals = await context.Set<InstrumentRental>()
            .AsNoTracking()
            .Include(x => x.Instrument)
            .Include(x => x.StudentProfile)
            .Where(x => x.Instrument.MusicStoreId == storeId)
            .OrderByDescending(x => x.RequestedAt)
            .ToListAsync(cancellationToken);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Text($"Pregled iznajmljivanja — {storeName}").Bold().FontSize(18);
                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(35);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("ID");
                        header.Cell().Element(CellStyle).Text("Instrument");
                        header.Cell().Element(CellStyle).Text("Status");
                        header.Cell().Element(CellStyle).Text("Naknada");
                        header.Cell().Element(CellStyle).Text("Ukupno");
                    });

                    foreach (var rental in rentals)
                    {
                        var dto = new InstrumentRentalDto
                        {
                            Fee = rental.Fee,
                            RentalStatus = rental.RentalStatus
                        };
                        RentalBilling.ApplyBilling(rental, dto, clock.UtcNow);

                        table.Cell().Element(CellStyle).Text(rental.Id.ToString());
                        table.Cell().Element(CellStyle).Text(rental.Instrument.Model);
                        table.Cell().Element(CellStyle).Text(rental.RentalStatus.ToString());
                        table.Cell().Element(CellStyle).Text(rental.Fee.ToString("F2", ReportCulture));
                        table.Cell().Element(CellStyle).Text(dto.TotalFee?.ToString("F2", ReportCulture) ?? "-");
                    }
                });
                page.Footer().AlignRight().Text($"Generisano: {clock.UtcNow:dd.MM.yyyy HH:mm} UTC").FontSize(9);
            });
        }).GeneratePdf();
    }

    public async Task<byte[]> GenerateLectureAttendancePdfAsync(int lectureId, CancellationToken cancellationToken = default)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(actor.UserId);
        var lecture = await instructorAccess.GetOwnedLectureAsync(lectureId, instructorId, includeAttendances: true);

        var nameMap = await displayNames.GetStudentDisplayNamesAsync(lecture.Attendances.Select(a => a.Student).Where(s => s is not null)!);

        var rows = lecture.Attendances.OrderBy(a => a.StudentId).Select(a =>
            new AttendanceRow(a.StudentId, nameMap.GetValueOrDefault(a.StudentId, $"Student {a.StudentId}"), a.AttendanceStatus)).ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Column(column =>
                {
                    column.Item().Text($"Prisustvo — {lecture.Name}").Bold().FontSize(18);
                    column.Item().Text($"{lecture.LectureTime:dd.MM.yyyy HH:mm} · {lecture.Location}").FontSize(11);
                });
                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Student");
                        header.Cell().Element(CellStyle).Text("Status");
                    });

                    foreach (var row in rows)
                    {
                        table.Cell().Element(CellStyle).Text(row.StudentName);
                        table.Cell().Element(CellStyle).Text(row.Status.ToString());
                    }
                });
                page.Footer().AlignRight().Text($"Generisano: {clock.UtcNow:dd.MM.yyyy HH:mm} UTC").FontSize(9);
            });
        }).GeneratePdf();
    }

    private static IContainer CellStyle(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(2);

    private sealed record AttendanceRow(int StudentId, string StudentName, AttendanceStatus Status);
}

```

---

## File: `eNote\eNote.Application\Validation\AddressRequestValidator.cs`
**Hash**: `0c1b8718a149` | **Size**: 448 chars

**Classes**: AddressRequestValidator
```cs
using eNote.Application.Features.Rentals.ReferenceData.Addresses;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class AddressRequestValidator : AbstractValidator<AddressRequest>
{
    public AddressRequestValidator()
    {
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Number).NotEmpty().MaximumLength(20);
    }
}

```

---

## File: `eNote\eNote.Application\Validation\AnnouncementRequestValidator.cs`
**Hash**: `c1770aa33a09` | **Size**: 362 chars

**Classes**: AnnouncementRequestValidator
```cs
using eNote.Application.Features.Communication.Announcements;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class AnnouncementRequestValidator : AbstractValidator<AnnouncementRequest>
{
    public AnnouncementRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.Content).NotEmpty();
    }
}

```

---

## File: `eNote\eNote.Application\Validation\AssignmentRequestValidator.cs`
**Hash**: `fe41b133b38d` | **Size**: 395 chars

**Classes**: AssignmentRequestValidator
```cs
using eNote.Application.Features.Academic.Assignments;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class AssignmentRequestValidator : AbstractValidator<AssignmentRequest>
{
    public AssignmentRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.DueAt).NotEmpty();
    }
}

```

---

## File: `eNote\eNote.Application\Validation\ChangePasswordRequestValidator.cs`
**Hash**: `f0ba77db32a5` | **Size**: 506 chars

**Classes**: ChangePasswordRequestValidator
```cs
using eNote.Application.Features.Identity.Users;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
        RuleFor(x => x.ConfirmNewPassword).NotEmpty().Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}

```

---

## File: `eNote\eNote.Application\Validation\CourseRequestValidator.cs`
**Hash**: `659d79c716e0` | **Size**: 386 chars

**Classes**: CourseRequestValidator
```cs
using eNote.Application.Features.Academic.Courses;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class CourseRequestValidator : AbstractValidator<CourseRequest>
{
    public CourseRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("Price must be non-negative.");
    }
}

```

---

## File: `eNote\eNote.Application\Validation\ForgotPasswordRequestValidator.cs`
**Hash**: `121af66d243b` | **Size**: 325 chars

**Classes**: ForgotPasswordRequestValidator
```cs
using eNote.Application.Features.Identity.Auth;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

```

---

## File: `eNote\eNote.Application\Validation\GradeAssignmentRequestValidator.cs`
**Hash**: `0f1a189a05cf` | **Size**: 334 chars

**Classes**: GradeAssignmentRequestValidator
```cs
using eNote.Application.Features.Academic.Assignments;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class GradeAssignmentRequestValidator : AbstractValidator<GradeAssignmentRequest>
{
    public GradeAssignmentRequestValidator()
    {
        RuleFor(x => x.Grade).InclusiveBetween(0, 100);
    }
}

```

---

## File: `eNote\eNote.Application\Validation\InstrumentCreateRequestValidator.cs`
**Hash**: `85ec024cd732` | **Size**: 428 chars

**Classes**: InstrumentCreateRequestValidator
```cs
using eNote.Application.Features.Rentals.Instruments;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class InstrumentCreateRequestValidator : AbstractValidator<InstrumentCreateRequest>
{
    public InstrumentCreateRequestValidator()
    {
        RuleFor(x => x.Model).NotEmpty();
        RuleFor(x => x.Manufacturer).NotEmpty();
        RuleFor(x => x.InstrumentTypeId).GreaterThan(0);
    }
}

```

---

## File: `eNote\eNote.Application\Validation\InstrumentTypeRequestValidator.cs`
**Hash**: `ac955a288ad3` | **Size**: 412 chars

**Classes**: InstrumentTypeRequestValidator
```cs
using eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class InstrumentTypeRequestValidator : AbstractValidator<InstrumentTypeRequest>
{
    public InstrumentTypeRequestValidator()
    {
        RuleFor(x => x.Type).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MonthlyFee).GreaterThanOrEqualTo(0);
    }
}

```

---

## File: `eNote\eNote.Application\Validation\LectureCreateRequestValidator.cs`
**Hash**: `66feac6039cd` | **Size**: 585 chars

**Classes**: LectureCreateRequestValidator
```cs
using eNote.Application.Common.Localization;
using eNote.Application.Features.Academic.Lectures;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class LectureCreateRequestValidator : AbstractValidator<LectureCreateRequest>
{
    public LectureCreateRequestValidator()
    {
        RuleFor(x => x.CourseId).GreaterThan(0).WithMessage(Messages.CourseIdRequired);
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Location).NotEmpty();
        RuleFor(x => x.LectureTime).NotEmpty();
        RuleFor(x => x.Duration).GreaterThan(0);
    }
}

```

---

## File: `eNote\eNote.Application\Validation\LectureNoteRequestValidator.cs`
**Hash**: `a0fea593ac04` | **Size**: 353 chars

**Classes**: LectureNoteRequestValidator
```cs
using eNote.Application.Features.Academic.LectureNotes;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class LectureNoteRequestValidator : AbstractValidator<LectureNoteRequest>
{
    public LectureNoteRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.Content).NotEmpty();
    }
}

```

---

## File: `eNote\eNote.Application\Validation\LectureUpdateRequestValidator.cs`
**Hash**: `df623ecc7934` | **Size**: 452 chars

**Classes**: LectureUpdateRequestValidator
```cs
using eNote.Application.Features.Academic.Lectures;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class LectureUpdateRequestValidator : AbstractValidator<LectureUpdateRequest>
{
    public LectureUpdateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Location).NotEmpty();
        RuleFor(x => x.LectureTime).NotEmpty();
        RuleFor(x => x.Duration).GreaterThan(0);
    }
}

```

---

## File: `eNote\eNote.Application\Validation\LoginRequestValidator.cs`
**Hash**: `98b762e15651` | **Size**: 410 chars

**Classes**: LoginRequestValidator
```cs
using eNote.Application.Features.Identity.Auth;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Korisničko ime je obavezno.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Lozinka je obavezna.");
    }
}

```

---

## File: `eNote\eNote.Application\Validation\MusicStoreRequestValidator.cs`
**Hash**: `1dde7eec2515` | **Size**: 409 chars

**Classes**: MusicStoreRequestValidator
```cs
using eNote.Application.Features.Rentals.ReferenceData.MusicStores;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class MusicStoreRequestValidator : AbstractValidator<MusicStoreRequest>
{
    public MusicStoreRequestValidator()
    {
        RuleFor(x => x.StoreName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BusinessHours).NotEmpty().MaximumLength(50);
    }
}

```

---

## File: `eNote\eNote.Application\Validation\RegisterRequestValidator.cs`
**Hash**: `1b4a7a6488d8` | **Size**: 414 chars

**Classes**: RegisterRequestValidator
```cs
using eNote.Application.Features.Identity.Auth;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

```

---

## File: `eNote\eNote.Application\Validation\RentalCreateRequestValidator.cs`
**Hash**: `793fb49bae38` | **Size**: 327 chars

**Classes**: RentalCreateRequestValidator
```cs
using eNote.Application.Features.Rentals.InstrumentRentals;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class RentalCreateRequestValidator : AbstractValidator<RentalCreateRequest>
{
    public RentalCreateRequestValidator()
    {
        RuleFor(x => x.InstrumentId).GreaterThan(0);
    }
}

```

---

## File: `eNote\eNote.Application\Validation\ResetPasswordRequestValidator.cs`
**Hash**: `3473c1b45873` | **Size**: 429 chars

**Classes**: ResetPasswordRequestValidator
```cs
using eNote.Application.Features.Identity.Auth;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}

```

---

## File: `eNote\eNote.Application\Validation\UpdateProfileRequestValidator.cs`
**Hash**: `c9e8780f0d84` | **Size**: 323 chars

**Classes**: UpdateProfileRequestValidator
```cs
using eNote.Application.Features.Identity.Users;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

```

---

## File: `eNote\eNote.Contracts\Rentals\RentalStatusChanged.cs`
**Hash**: `bffe586f8d50` | **Size**: 212 chars

```cs
namespace eNote.Contracts.Rentals;

public record RentalStatusChanged(int RentalId, int StudentUserId, int? ActorUserId, string Status, string InstrumentModel, string Title, string Body, DateTime OccurredAtUtc);

```

---

## File: `eNote\eNote.Domain\Entities\Academic\Attendance.cs`
**Hash**: `d85fcc6710fb` | **Size**: 743 chars

**Classes**: Attendance
```cs
using eNote.Domain.Entities;
using eNote.Domain.Enums;

namespace eNote.Domain.Entities;

public class Attendance : AuditableEntity
{
    public int StudentId { get; private set; }
    public Student Student { get; private set; } = null!;
    public int LectureId { get; private set; }
    public Lecture Lecture { get; private set; } = null!;

    public AttendanceStatus AttendanceStatus { get; private set; }

    protected Attendance()
    {
    }

    public Attendance(int studentId, int lectureId, AttendanceStatus status)
    {
        StudentId = studentId;
        LectureId = lectureId;
        AttendanceStatus = status;
    }

    public void UpdateStatus(AttendanceStatus status)
    {
        AttendanceStatus = status;
    }
}

```

---

## File: `eNote\eNote.Domain\Entities\Academic\Course.cs`
**Hash**: `b5a7f95ffca9` | **Size**: 1636 chars

**Classes**: Course
```cs
using eNote.Domain.Entities;

namespace eNote.Domain.Entities;

public class Course : AuditableEntity
{
    public int InstructorId { get; private set; }
    public Instructor Instructor { get; private set; } = null!;

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }

    public DateTime? StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public bool IsPublished { get; private set; }
    public bool IsActive { get; private set; } = true;

    public ICollection<Enrollment> Enrollments { get; private set; } = new List<Enrollment>();
    public ICollection<Lecture> Lectures { get; private set; } = new List<Lecture>();

    protected Course()
    {
    }

    public Course(string name, string? description, decimal price, DateTime? startDate, DateTime? endDate, int instructorId)
    {
        Name = name;
        Description = description;
        Price = price;
        StartDate = startDate;
        EndDate = endDate;
        InstructorId = instructorId;
        IsPublished = false;
        IsActive = true;
    }

    public void UpdateDetails(string name, string? description, decimal price, DateTime? startDate, DateTime? endDate)
    {
        Name = name;
        Description = description;
        Price = price;
        StartDate = startDate;
        EndDate = endDate;
    }

    public void SetPublishedStatus(bool isPublished)
    {
        IsPublished = isPublished;
    }

    public void SoftDelete()
    {
        IsActive = false;
        IsPublished = false;
    }
}

```

---

## File: `eNote\eNote.Domain\Entities\Academic\Enrollment.cs`
**Hash**: `d8ebfde57c29` | **Size**: 737 chars

**Classes**: Enrollment
```cs
using eNote.Domain.Entities;
using eNote.Domain.Enums;

namespace eNote.Domain.Entities;

public class Enrollment : AuditableEntity
{
    public int StudentId { get; private set; }
    public Student Student { get; private set; } = null!;
    public int CourseId { get; private set; }
    public Course Course { get; private set; } = null!;

    public EnrollmentStatus EnrollmentStatus { get; private set; }

    protected Enrollment()
    {
    }

    public Enrollment(int studentId, int courseId, EnrollmentStatus status)
    {
        StudentId = studentId;
        CourseId = courseId;
        EnrollmentStatus = status;
    }

    public void UpdateStatus(EnrollmentStatus status)
    {
        EnrollmentStatus = status;
    }
}

```

---

## File: `eNote\eNote.Domain\Entities\Academic\Lecture.cs`
**Hash**: `24ad7af30a5e` | **Size**: 1968 chars

**Classes**: Lecture
```cs
using eNote.Domain.Entities;
using eNote.Domain.Enums;

namespace eNote.Domain.Entities;

public class Lecture : AuditableEntity
{
    public int CourseId { get; private set; }
    public Course Course { get; private set; } = null!;

    public string Name { get; private set; } = null!;
    public string Location { get; private set; } = null!;
    public LectureType LectureType { get; private set; }

    public DateTime LectureTime { get; private set; }
    public int Duration { get; private set; }
    public int? Capacity { get; private set; }

    public LectureStatus LectureStatus { get; private set; }
    public bool IsCancelled => LectureStatus == LectureStatus.Cancelled;
    public bool IsActive { get; private set; } = true;
    public byte[]? RowVersion { get; set; }

    public ICollection<Attendance> Attendances { get; private set; } = new List<Attendance>();
    public ICollection<LectureNote> LectureNotes { get; private set; } = new List<LectureNote>();
    public ICollection<Assignment> Assignments { get; private set; } = new List<Assignment>();

    protected Lecture()
    {
    }

    public Lecture(string name, string location, int duration, DateTime lectureTime, LectureType lectureType, int? capacity, int courseId)
    {
        Name = name;
        Location = location;
        Duration = duration;
        LectureTime = lectureTime;
        LectureType = lectureType;
        Capacity = capacity;
        CourseId = courseId;
        LectureStatus = LectureStatus.Scheduled;
        IsActive = true;
    }

    public void UpdateDetails(string name, string location, int duration, DateTime lectureTime, int? capacity)
    {
        Name = name;
        Location = location;
        Duration = duration;
        LectureTime = lectureTime;
        Capacity = capacity;
    }

    public void Cancel()
    {
        LectureStatus = LectureStatus.Cancelled;
    }

    public void SoftDelete()
    {
        IsActive = false;
    }
}

```

---

## File: `eNote\eNote.Domain\Entities\Academic\LectureNote.cs`
**Hash**: `acc4423cf7e1` | **Size**: 803 chars

**Classes**: LectureNote
```cs
using eNote.Domain.Entities;

namespace eNote.Domain.Entities;

public class LectureNote : AuditableEntity
{
    public int LectureId { get; private set; }
    public Lecture Lecture { get; private set; } = null!;

    public string Title { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;

    protected LectureNote()
    {
    }

    public LectureNote(string title, string content, int lectureId)
    {
        Title = title;
        Content = content;
        LectureId = lectureId;
        IsActive = true;
    }

    public void UpdateDetails(string title, string content)
    {
        Title = title;
        Content = content;
    }

    public void SoftDelete()
    {
        IsActive = false;
    }
}

```

---

## File: `eNote\eNote.Domain\Entities\Communication\Announcement.cs`
**Hash**: `c92705eb215a` | **Size**: 1386 chars

**Classes**: Announcement
```cs
using eNote.Domain.Entities;

namespace eNote.Domain.Entities;

public class Announcement : AuditableEntity
{
    public int? CourseId { get; private set; }
    public Course? Course { get; private set; }
    public int? MusicStoreId { get; private set; }
    public MusicStore? MusicStore { get; private set; }

    public string Title { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public string? ImagePath { get; private set; }

    public DateTime PublishedAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    protected Announcement()
    {
    }

    public Announcement(string title, string content, int? courseId, int? musicStoreId, DateTime publishedAt, string? imagePath = null)
    {
        Title = title;
        Content = content;
        ImagePath = imagePath;
        CourseId = courseId;
        MusicStoreId = musicStoreId;
        PublishedAt = publishedAt;
        IsActive = true;
    }

    public void UpdateDetails(string title, string content, string? imagePath = null)
    {
        Title = title;
        Content = content;

        if (imagePath is not null)
        {
            ImagePath = imagePath;
        }
    }

    public void SetImagePath(string? imagePath)
    {
        ImagePath = imagePath;
    }

    public void SoftDelete()
    {
        IsActive = false;
    }
}

```

---

## File: `eNote\eNote.Domain\Entities\Communication\Notification.cs`
**Hash**: `325461e917ae` | **Size**: 755 chars

**Classes**: Notification
```cs
namespace eNote.Domain.Entities;

public class Notification
{
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int? RentalId { get; private set; }

    public string Title { get; private set; } = null!;
    public string Body { get; private set; } = null!;

    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }

    protected Notification()
    {
    }

    public Notification(int userId, string title, string body, DateTime createdAt, int? rentalId = null)
    {
        UserId = userId;
        Title = title;
        Body = body;
        CreatedAt = createdAt;
        RentalId = rentalId;
    }

    public void MarkRead()
    {
        IsRead = true;
    }
}

```

---

## File: `eNote\eNote.Domain\Entities\Communication\RentalNotificationOutbox.cs`
**Hash**: `bf33479fde39` | **Size**: 304 chars

**Classes**: RentalNotificationOutbox
### Key Cross-Cutting Interactions
- Uses **RabbitMQ|IEventBus|Outbox** → Async integration events (RabbitMQ)

```cs
using eNote.Domain.Entities;

namespace eNote.Domain.Entities;

public class RentalNotificationOutbox : AuditableEntity
{
    public string PayloadJson { get; set; } = null!;
    public DateTime? PublishedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
```

---

## File: `eNote\eNote.Domain\Entities\Identity\Instructor.cs`
**Hash**: `90a78f17b900` | **Size**: 362 chars

**Classes**: Instructor
```cs
using eNote.Domain.Entities;

namespace eNote.Domain.Entities;

public class Instructor : AuditableEntity
{
    public int AppUserId { get; private set; }

    public ICollection<Course> Courses { get; private set; } = new List<Course>();

    protected Instructor()
    {
    }

    public Instructor(int appUserId)
    {
        AppUserId = appUserId;
    }
}

```

---

## File: `eNote\eNote.Domain\Entities\Identity\MusicStoreEmployee.cs`
**Hash**: `4af84e225fda` | **Size**: 614 chars

**Classes**: MusicStoreEmployee
```cs
using eNote.Domain.Entities;

namespace eNote.Domain.Entities;

public class MusicStoreEmployee : AuditableEntity
{
    public int AppUserId { get; private set; }
    public int MusicStoreId { get; private set; }
    public MusicStore MusicStore { get; private set; } = null!;

    public bool IsManager { get; private set; }
    public bool IsActive { get; set; } = true;

    protected MusicStoreEmployee()
    {
    }

    public MusicStoreEmployee(int appUserId, int musicStoreId, bool isManager)
    {
        AppUserId = appUserId;
        MusicStoreId = musicStoreId;
        IsManager = isManager;
    }
}

```

---

## File: `eNote\eNote.Domain\Entities\Identity\RevokedToken.cs`
**Hash**: `f57fb207c81d` | **Size**: 237 chars

**Classes**: RevokedToken
```cs
namespace eNote.Domain.Entities;

public class RevokedToken
{
    public int Id { get; set; }
    public string Jti { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime RevokedAt { get; set; }
}

```

---

## File: `eNote\eNote.Domain\Entities\Identity\Student.cs`
**Hash**: `6cca9ab9d45e` | **Size**: 1152 chars

**Classes**: Student
```cs
using eNote.Domain.Entities;

namespace eNote.Domain.Entities;

public class Student : AuditableEntity
{
    public int AppUserId { get; private set; }
    public DateTime EnrollmentDate { get; private set; }
    public DateTime? MembershipPaidUntil { get; private set; }

    public ICollection<Attendance> Attendances { get; private set; } = new List<Attendance>();
    public ICollection<Enrollment> Enrollments { get; private set; } = new List<Enrollment>();
    public ICollection<InstrumentRental> InstrumentRentals { get; private set; } = new List<InstrumentRental>();
    public ICollection<AssignmentSubmission> AssignmentSubmissions { get; private set; } = new List<AssignmentSubmission>();

    protected Student()
    {
    }

    public Student(int appUserId, DateTime enrollmentDate)
    {
        AppUserId = appUserId;
        EnrollmentDate = enrollmentDate;
    }

    public void UpdateMembership(DateTime? paidUntil)
    {
        MembershipPaidUntil = paidUntil;
    }

    public bool HasActiveMembership(DateTime utcNow)
    {
        return MembershipPaidUntil.HasValue && MembershipPaidUntil.Value.Date >= utcNow.Date;
    }
}

```

---

## File: `eNote\eNote.Domain\Entities\Rentals\Instrument.cs`
**Hash**: `45de9e036b2b` | **Size**: 1608 chars

**Classes**: Instrument
```cs
using eNote.Domain.Entities;
using eNote.Domain.Enums;

namespace eNote.Domain.Entities;

public class Instrument : AuditableEntity
{
    public int InstrumentTypeId { get; private set; }
    public InstrumentType InstrumentType { get; private set; } = null!;
    public int MusicStoreId { get; private set; }
    public MusicStore MusicStore { get; private set; } = null!;

    public string Model { get; private set; } = null!;
    public string Manufacturer { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? ImagePath { get; private set; }

    public bool IsActive { get; private set; } = true;
    public bool IsAvailable =>
        IsActive && !InstrumentRentals.Any(x => x.RentalStatus.BlocksInstrument());

    public ICollection<InstrumentRental> InstrumentRentals { get; private set; } = [];

    protected Instrument() { }

    public Instrument(string model, string manufacturer, string? description, string? imagePath, int instrumentTypeId, int musicStoreId)
    {
        Model = model;
        Manufacturer = manufacturer;
        Description = description;
        ImagePath = imagePath;
        InstrumentTypeId = instrumentTypeId;
        MusicStoreId = musicStoreId;
    }

    public void UpdateDetails(string model, string manufacturer, string? description, string? imagePath, int instrumentTypeId)
    {
        Model = model;
        Manufacturer = manufacturer;
        Description = description;
        ImagePath = imagePath;
        InstrumentTypeId = instrumentTypeId;
    }

    public void SoftDelete() => IsActive = false;
}
```

---

## File: `eNote\eNote.Domain\Entities\Rentals\InstrumentRental.cs`
**Hash**: `b18ae6f1c964` | **Size**: 2624 chars

**Classes**: InstrumentRental
```cs
using eNote.Domain.Entities;
using eNote.Domain.Enums;

namespace eNote.Domain.Entities;

public class InstrumentRental : AuditableEntity
{
    public int StudentProfileId { get; private set; }
    public Student StudentProfile { get; private set; } = null!;
    public int InstrumentId { get; private set; }
    public Instrument Instrument { get; private set; } = null!;

    public InstrumentRentalStatus RentalStatus { get; private set; }
    public string? RequestNote { get; private set; }
    public string? Note { get; private set; }

    public DateTime RequestedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? RejectedAt { get; private set; }
    public DateTime? PickedUpAt { get; private set; }
    public DateTime? ReturnedAt { get; private set; }

    public int? ApprovedById { get; private set; }
    public int? RejectedById { get; private set; }

    public decimal Fee { get; private set; }

    protected InstrumentRental()
    {
    }

    public InstrumentRental(int instrumentId, int studentProfileId, DateTime requestedAt, string? note)
    {
        InstrumentId = instrumentId;
        StudentProfileId = studentProfileId;
        RequestedAt = requestedAt;
        RequestNote = note;
        RentalStatus = InstrumentRentalStatus.Pending;
    }

    public void Approve(decimal fee, string? note, DateTime approvedAt, int approvedById)
    {
        Fee = fee;
        Note = note;
        ApprovedAt = approvedAt;
        ApprovedById = approvedById;
        RentalStatus = InstrumentRentalStatus.Approved;
    }

    public void Reject(DateTime rejectedAt, string? note, int rejectedById)
    {
        Note = note;
        RejectedAt = rejectedAt;
        RejectedById = rejectedById;
        RentalStatus = InstrumentRentalStatus.Rejected;
    }

    public void Cancel(DateTime returnedAt, string? note)
    {
        Note = note;
        ReturnedAt = returnedAt;
        RentalStatus = InstrumentRentalStatus.Canceled;
    }

    public void Pickup(DateTime pickedUpAt, string? note = null)
    {
        PickedUpAt = pickedUpAt;
        RentalStatus = InstrumentRentalStatus.Active;
        if (note != null)
        {
            Note = note;
        }
    }

    public void Complete(DateTime returnedAt, string? note)
    {
        Note = note;
        ReturnedAt = returnedAt;
        RentalStatus = InstrumentRentalStatus.Completed;
    }

    public void ReturnEarly(DateTime returnedAt, string? note)
    {
        Note = note;
        ReturnedAt = returnedAt;
        RentalStatus = InstrumentRentalStatus.ReturnedEarly;
    }
}

```

---

## File: `eNote\eNote.Domain\Entities\Rentals\InstrumentType.cs`
**Hash**: `2d9c4ed1e867` | **Size**: 330 chars

**Classes**: InstrumentType
```cs
using eNote.Domain.Entities;

namespace eNote.Domain.Entities;

public class InstrumentType : BaseEntity
{
    private readonly List<Instrument> _instruments = [];

    public string Type { get; set; } = null!;
    public decimal MonthlyFee { get; set; }

    public IReadOnlyCollection<Instrument> Instruments => _instruments;
}

```

---

## File: `eNote\eNote.Domain\Entities\Rentals\InstrumentView.cs`
**Hash**: `957d28ebe538` | **Size**: 666 chars

**Classes**: InstrumentView
```cs
namespace eNote.Domain.Entities;

public class InstrumentView
{
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int InstrumentId { get; private set; }

    public int ViewCount { get; private set; }
    public DateTime LastViewedAt { get; private set; }

    protected InstrumentView()
    {
    }

    public InstrumentView(int userId, int instrumentId, DateTime viewedAt)
    {
        UserId = userId;
        InstrumentId = instrumentId;
        ViewCount = 1;
        LastViewedAt = viewedAt;
    }

    public void RecordView(DateTime viewedAt)
    {
        ViewCount++;
        LastViewedAt = viewedAt;
    }
}

```

---

## File: `eNote\eNote.Domain\Entities\Rentals\MusicStore.cs`
**Hash**: `d64b6130e7b6` | **Size**: 840 chars

**Classes**: MusicStore
```cs
using eNote.Domain.Entities;

namespace eNote.Domain.Entities;

public class MusicStore : AuditableEntity
{
    private readonly List<MusicStoreEmployee> _employees = [];
    private readonly List<Instrument> _instruments = [];

    public string StoreName { get; private set; } = null!;
    public string BusinessHours { get; private set; } = null!;

    public IReadOnlyCollection<MusicStoreEmployee> Employees => _employees;
    public IReadOnlyCollection<Instrument> Instruments => _instruments;

    protected MusicStore()
    {
    }

    public MusicStore(string storeName, string businessHours)
    {
        StoreName = storeName;
        BusinessHours = businessHours;
    }

    public void UpdateDetails(string storeName, string businessHours)
    {
        StoreName = storeName;
        BusinessHours = businessHours;
    }
}

```

---

## File: `eNote\eNote.Domain\Entities\Shared\Address.cs`
**Hash**: `84e7c8a7d5fb` | **Size**: 244 chars

**Classes**: Address
```cs
using eNote.Domain.Entities;

namespace eNote.Domain.Entities;

public class Address : BaseEntity
{
    public string City { get; set; } = null!;
    public string Street { get; set; } = null!;
    public string Number { get; set; } = null!;
}

```

---

## File: `eNote\eNote.Domain\Entities\Shared\Base\BaseEntity.cs`
**Hash**: `ea2eb5eb597b` | **Size**: 343 chars

**Classes**: AuditableEntity, BaseEntity
```cs
namespace eNote.Domain.Entities;

public abstract class BaseEntity : IEntity
{
    public int Id { get; set; }
}

public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public int? CreatedById { get; set; }
    public int? UpdatedById { get; set; }
}

```

---

## File: `eNote\eNote.Domain\Entities\Shared\Base\IEntity.cs`
**Hash**: `f0e4088c7d87` | **Size**: 88 chars

**Classes**: 
**Interfaces**: IEntity
```cs
namespace eNote.Domain.Entities;

public interface IEntity
{
    int Id { get; set; }
}

```

---

## File: `eNote\eNote.Domain\Enums\AnnouncementScope.cs`
**Hash**: `4aca5aa05dd6` | **Size**: 100 chars

```cs
namespace eNote.Domain.Enums;

public enum AnnouncementScope
{
    Course = 1,
    MusicStore = 2
}

```

---

## File: `eNote\eNote.Domain\Enums\AttendanceStatus.cs`
**Hash**: `40474a91c1f9` | **Size**: 113 chars

```cs
namespace eNote.Domain.Enums;

public enum AttendanceStatus
{
    Pending = 1,
    Present = 2,
    Absent = 3
}

```

---

## File: `eNote\eNote.Domain\Enums\EnrollmentStatus.cs`
**Hash**: `1b7ea1fadda4` | **Size**: 116 chars

```cs
namespace eNote.Domain.Enums;

public enum EnrollmentStatus
{
    Active = 1,
    Completed = 2,
    Canceled = 3
}

```

---

## File: `eNote\eNote.Domain\Enums\LectureStatus.cs`
**Hash**: `a520232c220d` | **Size**: 112 chars

```cs
namespace eNote.Domain.Enums;

public enum LectureStatus
{
    Scheduled = 1,
    Held = 2,
    Cancelled = 3
}

```

---

## File: `eNote\eNote.Domain\Enums\LectureType.cs`
**Hash**: `352c75864656` | **Size**: 117 chars

```cs
namespace eNote.Domain.Enums;

public enum LectureType
{
    Theoretical = 1,
    Practical = 2,
    Combined = 3,
}

```

---

## File: `eNote\eNote.Infrastructure\Configuration\DotEnvConfiguration.cs`
**Hash**: `5b465ef76902` | **Size**: 523 chars

**Classes**: DotEnvConfiguration
```cs
using DotNetEnv;

namespace eNote.Infrastructure.Configuration;

public static class DotEnvConfiguration
{
    public static void Load()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory is not null)
        {
            var envFile = Path.Combine(directory.FullName, ".env");

            if (File.Exists(envFile))
            {
                Env.Load(envFile);
                return;
            }

            directory = directory.Parent;
        }
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\Configurations\AddressConfig.cs`
**Hash**: `d1be0f3742b3` | **Size**: 524 chars

**Classes**: AddressConfig
```cs
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class AddressConfig : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.Property(a => a.City).HasStringConfig(100, true);
        builder.Property(a => a.Street).HasStringConfig(100, true);
        builder.Property(a => a.Number).HasStringConfig(20, true);
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\Configurations\AnnouncementConfig.cs`
**Hash**: `87ae38403ae8` | **Size**: 1508 chars

**Classes**: AnnouncementConfig
```cs
using eNote.Domain.Entities;
using eNote.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class AnnouncementConfig : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.Property(x => x.Title).HasStringConfig(150, true);
        builder.Property(x => x.Content).HasStringConfig(4000, true);
        builder.Property(x => x.ImagePath).HasMaxLength(500);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.HasQueryFilter(x => x.IsActive);

        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Course)
            .WithMany()
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MusicStore)
            .WithMany()
            .HasForeignKey(x => x.MusicStoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.PublishedAt);
        builder.HasIndex(x => x.CourseId);
        builder.HasIndex(x => x.MusicStoreId);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Announcement_Scope",
            "([CourseId] IS NOT NULL AND [MusicStoreId] IS NULL) OR ([CourseId] IS NULL AND [MusicStoreId] IS NOT NULL)"));
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\Configurations\AppUserConfig.cs`
**Hash**: `b811f371cc6c` | **Size**: 496 chars

**Classes**: AppUserConfig
```cs
using eNote.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class AppUserConfig : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.HasOne(u => u.Address)
               .WithMany()
               .HasForeignKey(u => u.AddressId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\Configurations\AssignmentConfig.cs`
**Hash**: `6aa9a30ebfa9` | **Size**: 755 chars

**Classes**: AssignmentConfig
```cs
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class AssignmentConfig : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.HasOne(a => a.Lecture)
               .WithMany(l => l.Assignments)
               .HasForeignKey(a => a.LectureId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Property(a => a.Title).HasStringConfig(200, true);
        builder.Property(a => a.Description).IsRequired();
        builder.Property(a => a.IsActive).HasDefaultValue(true);
        builder.HasQueryFilter(a => a.IsActive);
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\Configurations\AttendanceConfig.cs`
**Hash**: `e85462b9e220` | **Size**: 905 chars

**Classes**: AttendanceConfig
```cs
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class AttendanceConfig : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.HasQueryFilter(a => a.Lecture.IsActive);

        builder.HasOne(p => p.Student)
               .WithMany(s => s.Attendances)
               .HasForeignKey(p => p.StudentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Lecture)
               .WithMany(p => p.Attendances)
               .HasForeignKey(p => p.LectureId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.StudentId, p.LectureId }).IsUnique();
        builder.Property(p => p.AttendanceStatus).HasConversion<int>();
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\Configurations\ConfigurationHelpers.cs`
**Hash**: `3a6f8e08685e` | **Size**: 1515 chars

**Classes**: ConfigurationHelpers
```cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public static class ConfigurationHelpers
{
    public static PropertyBuilder<string> HasStringConfig(this PropertyBuilder<string> propertyBuilder, int? maxLength = null, bool isRequired = false)
    {
        if (isRequired)
        {
            propertyBuilder.IsRequired();
        }

        if (maxLength.HasValue)
        {
            propertyBuilder.HasMaxLength(maxLength.Value);
        }

        return propertyBuilder;
    }

    public static PropertyBuilder<decimal> HasDecimalPrecision(this PropertyBuilder<decimal> propertyBuilder, int precision = 18, int scale = 2)
    {
        return propertyBuilder.HasPrecision(precision, scale);
    }

    public static PropertyBuilder<bool> HasDefaultFalse(this PropertyBuilder<bool> propertyBuilder)
    {
        return propertyBuilder.HasDefaultValue(false);
    }

    public static PropertyBuilder<DateTime> HasDefaultSqlNow(this PropertyBuilder<DateTime> propertyBuilder)
    {
        return propertyBuilder.HasDefaultValueSql("GETUTCDATE()");
    }

    public static IndexBuilder HasUniqueIndex(this EntityTypeBuilder builder, string propertyName)
    {
        return builder.HasIndex(propertyName).IsUnique();
    }

    public static IndexBuilder HasUniqueIndex(this EntityTypeBuilder builder, params string[] propertyNames)
    {
        return builder.HasIndex(propertyNames).IsUnique();
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\Configurations\CourseConfig.cs`
**Hash**: `b2629b63a051` | **Size**: 924 chars

**Classes**: CourseConfig
```cs
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class CourseConfig : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasOne(x => x.Instructor)
               .WithMany(i => i.Courses)
               .HasForeignKey(x => x.InstructorId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lectures)
               .WithOne(x => x.Course)
               .HasForeignKey(x => x.CourseId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Price).HasDecimalPrecision();
        builder.Property(x => x.IsPublished).HasDefaultFalse();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.HasQueryFilter(x => x.IsActive);
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\Configurations\EnrollmentConfig.cs`
**Hash**: `5ca3836e090a` | **Size**: 902 chars

**Classes**: EnrollmentConfig
```cs
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class EnrollmentConfig : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.HasQueryFilter(e => e.Course.IsActive);

        builder.HasOne(x => x.Student)
               .WithMany(s => s.Enrollments)
               .HasForeignKey(x => x.StudentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Course)
               .WithMany(x => x.Enrollments)
               .HasForeignKey(x => x.CourseId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.StudentId, e.CourseId }).IsUnique();
        builder.Property(e => e.EnrollmentStatus).HasConversion<int>();
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\Configurations\InstructorConfig.cs`
**Hash**: `21d50f7b5110` | **Size**: 595 chars

**Classes**: InstructorConfig
```cs
using eNote.Domain.Entities;
using eNote.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class InstructorConfig : IEntityTypeConfiguration<Instructor>
{
    public void Configure(EntityTypeBuilder<Instructor> builder)
    {
        builder.HasOne<AppUser>()
               .WithOne()
               .HasForeignKey<Instructor>(i => i.AppUserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.AppUserId).IsUnique();
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\Configurations\InstrumentConfig.cs`
**Hash**: `83c5b3a1a9e5` | **Size**: 1020 chars

**Classes**: InstrumentConfig
```cs
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class InstrumentConfig : IEntityTypeConfiguration<Instrument>
{
    public void Configure(EntityTypeBuilder<Instrument> builder)
    {
        builder.HasOne(x => x.MusicStore)
               .WithMany(x => x.Instruments)
               .HasForeignKey(x => x.MusicStoreId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.InstrumentType)
               .WithMany(t => t.Instruments)
               .HasForeignKey(x => x.InstrumentTypeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Model).HasStringConfig(100, true);
        builder.Property(x => x.Manufacturer).HasStringConfig(100, true);
        builder.Property(x => x.Description).HasMaxLength(1000);

        builder.Ignore(x => x.IsAvailable);
        builder.HasQueryFilter(x => x.IsActive);
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\Configurations\InstrumentRentalConfig.cs`
**Hash**: `3f554f9ff6c4` | **Size**: 1380 chars

**Classes**: InstrumentRentalConfig
```cs
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class InstrumentRentalConfig : IEntityTypeConfiguration<InstrumentRental>
{
    public void Configure(EntityTypeBuilder<InstrumentRental> builder)
    {
        // intentionally excludes rentals for inactive instruments globally; use IgnoreQueryFilters() for historical/audit queries
        builder.HasQueryFilter(r => r.Instrument.IsActive);

        builder.HasOne(x => x.StudentProfile)
               .WithMany(s => s.InstrumentRentals)
               .HasForeignKey(x => x.StudentProfileId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Instrument)
               .WithMany(x => x.InstrumentRentals)
               .HasForeignKey(x => x.InstrumentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Fee).HasDecimalPrecision(10, 2).IsRequired();

        builder.Property(x => x.RentalStatus)
               .HasConversion<int>();

        builder.HasIndex(x => x.InstrumentId)
               .HasFilter(
                    $"[{nameof(InstrumentRental.RentalStatus)}] IN ({(int)InstrumentRentalStatus.Approved}, {(int)InstrumentRentalStatus.Active})"
               ).IsUnique();
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\Configurations\InstrumentTypeConfig.cs`
**Hash**: `a167942f6010` | **Size**: 482 chars

**Classes**: InstrumentTypeConfig
```cs
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class InstrumentTypeConfig : IEntityTypeConfiguration<InstrumentType>
{
    public void Configure(EntityTypeBuilder<InstrumentType> builder)
    {
        builder.Property(t => t.Type).HasStringConfig(100, true);
        builder.Property(t => t.MonthlyFee).HasDecimalPrecision(18, 2);
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\Configurations\InstrumentViewConfig.cs`
**Hash**: `286b8a9880a6` | **Size**: 467 chars

**Classes**: InstrumentViewConfig
```cs
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class InstrumentViewConfig : IEntityTypeConfiguration<InstrumentView>
{
    public void Configure(EntityTypeBuilder<InstrumentView> builder)
    {
        builder.HasIndex(x => new { x.UserId, x.InstrumentId }).IsUnique();
        builder.HasIndex(x => x.LastViewedAt);
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\Configurations\LectureConfig.cs`
**Hash**: `b404f226958c` | **Size**: 1167 chars

**Classes**: LectureConfig
```cs
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class LectureConfig : IEntityTypeConfiguration<Lecture>
{
    public void Configure(EntityTypeBuilder<Lecture> builder)
    {
        builder.HasOne(p => p.Course)
               .WithMany(k => k.Lectures)
               .HasForeignKey(p => p.CourseId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.Name).HasStringConfig(200, true);
        builder.Property(p => p.Location).HasStringConfig(200, true);
        builder.Property(p => p.Duration).IsRequired();
        builder.Property(p => p.LectureType).HasConversion<int>();
        builder.Property(p => p.LectureStatus).HasConversion<int>();
        builder.Property(p => p.LectureTime).IsRequired();
        builder.Property(p => p.Capacity).IsRequired(false);
        builder.Ignore(p => p.IsCancelled);
        builder.Property(p => p.IsActive).HasDefaultValue(true);
        builder.HasQueryFilter(p => p.IsActive);
        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\Configurations\LectureNoteConfig.cs`
**Hash**: `b14c71db45a6` | **Size**: 812 chars

**Classes**: LectureNoteConfig
```cs
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class LectureNoteConfig : IEntityTypeConfiguration<LectureNote>
{
    public void Configure(EntityTypeBuilder<LectureNote> builder)
    {
        builder.HasOne(n => n.Lecture)
               .WithMany(p => p.LectureNotes)
               .HasForeignKey(n => n.LectureId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Property(n => n.Title).HasStringConfig(200, true);
        builder.Property(n => n.Content).IsRequired();
        builder.Property(n => n.CreatedAt).IsRequired();
        builder.Property(n => n.IsActive).HasDefaultValue(true);
        builder.HasQueryFilter(n => n.IsActive);
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\Configurations\MusicStoreConfig.cs`
**Hash**: `af1920e47412` | **Size**: 854 chars

**Classes**: MusicStoreConfig
```cs
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class MusicStoreConfig : IEntityTypeConfiguration<MusicStore>
{
    public void Configure(EntityTypeBuilder<MusicStore> builder)
    {
        builder.Property(m => m.StoreName).HasStringConfig(100, true);
        builder.Property(m => m.BusinessHours).HasStringConfig(50, true);

        builder.HasMany(x => x.Employees)
               .WithOne(e => e.MusicStore)
               .HasForeignKey(e => e.MusicStoreId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Instruments)
               .WithOne(i => i.MusicStore)
               .HasForeignKey(i => i.MusicStoreId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\Configurations\MusicStoreEmployeeConfig.cs`
**Hash**: `66e7acce258b` | **Size**: 1029 chars

**Classes**: MusicStoreEmployeeConfig
```cs
using eNote.Domain.Entities;
using eNote.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public class MusicStoreEmployeeConfig : IEntityTypeConfiguration<MusicStoreEmployee>
{
    public void Configure(EntityTypeBuilder<MusicStoreEmployee> builder)
    {
        builder.HasUniqueIndex(nameof(MusicStoreEmployee.AppUserId));
        builder.HasUniqueIndex(nameof(MusicStoreEmployee.MusicStoreId), nameof(MusicStoreEmployee.AppUserId));

        builder.Property(x => x.IsManager).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasOne(x => x.MusicStore)
               .WithMany(x => x.Employees)
               .HasForeignKey(x => x.MusicStoreId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AppUser>()
               .WithMany()
               .HasForeignKey(x => x.AppUserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\Configurations\NotificationConfig.cs`
**Hash**: `840a8ab1d6db` | **Size**: 718 chars

**Classes**: NotificationConfig
```cs
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class NotificationConfig : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.IsRead).HasDefaultValue(false);

        builder.HasIndex(x => new { x.UserId, x.IsRead });
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.UserId, x.RentalId, x.Title });
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\Configurations\RentalNotificationOutboxConfig.cs`
**Hash**: `9e4ab6a8e806` | **Size**: 667 chars

**Classes**: RentalNotificationOutboxConfig
### Key Cross-Cutting Interactions
- Uses **RabbitMQ|IEventBus|Outbox** → Async integration events (RabbitMQ)

```cs
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class RentalNotificationOutboxConfig : IEntityTypeConfiguration<RentalNotificationOutbox>
{
    public void Configure(EntityTypeBuilder<RentalNotificationOutbox> builder)
    {
        builder.ToTable("RentalNotificationOutbox");

        builder.Property(x => x.PayloadJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.LastError)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.PublishedAt);
    }
}
```

---

## File: `eNote\eNote.Infrastructure\Data\Configurations\RevokedTokenConfig.cs`
**Hash**: `6a8e7a5f6645` | **Size**: 569 chars

**Classes**: RevokedTokenConfig
```cs
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class RevokedTokenConfig : IEntityTypeConfiguration<RevokedToken>
{
    public void Configure(EntityTypeBuilder<RevokedToken> builder)
    {
        builder.Property(x => x.Jti).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.Jti).IsUnique();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.RevokedAt).IsRequired();
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\Configurations\StudentConfig.cs`
**Hash**: `19350ff0225c` | **Size**: 583 chars

**Classes**: StudentConfig
```cs
using eNote.Domain.Entities;
using eNote.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eNote.Infrastructure.Data.Configurations;

public sealed class StudentConfig : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasOne<AppUser>()
               .WithOne()
               .HasForeignKey<Student>(s => s.AppUserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.AppUserId).IsUnique();
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\ENoteContext.cs`
**Hash**: `ed6d4798a7cb` | **Size**: 1603 chars

**Classes**: ENoteContext
### Key Cross-Cutting Interactions
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Infrastructure.Data.Seed;
using eNote.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace eNote.Infrastructure.Data;

public class ENoteContext(DbContextOptions<ENoteContext> options, IClock clock) : IdentityDbContext<AppUser, AppRole, int>(options), IAppDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ENoteContext).Assembly);

        ModelBuilderSeed.Seed(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;

        foreach (EntityEntry<AuditableEntity> entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await Database.BeginTransactionAsync(cancellationToken);
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\ENoteContextFactory.cs`
**Hash**: `ad245c241c99` | **Size**: 1427 chars

**Classes**: ENoteContextFactory
### Key Cross-Cutting Interactions
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
﻿using DotNetEnv;
using eNote.Application.Common.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace eNote.Infrastructure.Data;

public sealed class ENoteContextFactory : IDesignTimeDbContextFactory<ENoteContext>
{
    public ENoteContext CreateDbContext(string[] args)
    {
        LoadDotEnv();

        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings__DefaultConnection is missing. Set it in .env or environment variables.");

        var optionsBuilder = new DbContextOptionsBuilder<ENoteContext>();
        optionsBuilder.UseSqlServer(connectionString, sql => sql.MigrationsAssembly("eNote.Infrastructure"));

        return new ENoteContext(optionsBuilder.Options, new SystemClock());
    }

    private static void LoadDotEnv()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory is not null)
        {
            var envFile = Path.Combine(directory.FullName, ".env");

            if (File.Exists(envFile))
            {
                Env.Load(envFile);
                return;
            }

            directory = directory.Parent;
        }
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\Seed\DevelopmentDataSeed.cs`
**Hash**: `e9833cc19444` | **Size**: 10000 chars

**Classes**: AccessoriesInstruments, BrassInstruments, CourseSeed, DevelopmentDataSeed, EnrollmentSeed, InstrumentSeed, KeysInstruments, LectureSeed, PercussionInstruments, StringInstruments, StudentMembershipSeed
```cs
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Data.Seed;

public static class DevelopmentDataSeed
{
    public static async Task SeedAsync(ENoteContext context)
    {
        await CourseSeed.SeedCourses(context);
        await LectureSeed.SeedLectures(context);
        await InstrumentSeed.SeedInstruments(context);
        await EnrollmentSeed.SeedEnrollments(context);
        await StudentMembershipSeed.SeedMemberships(context);
    }
}

internal static class StudentMembershipSeed
{
    public static async Task SeedMemberships(ENoteContext context)
    {
        var students = await context.Set<Student>()
            .Where(s => s.MembershipPaidUntil == null)
            .ToListAsync();

        if (students.Count == 0)
        {
            return;
        }

        var paidUntil = DateTime.UtcNow.AddYears(1);

        foreach (Student student in students)
        {
            student.UpdateMembership(paidUntil);
        }

        await context.SaveChangesAsync();
    }
}

internal static class CourseSeed
{
    public static async Task SeedCourses(ENoteContext context)
    {
        if (await context.Set<Course>().AnyAsync())
        {
            return;
        }

        var instructorId = await context.Set<Instructor>()
            .OrderBy(i => i.Id)
            .Select(i => i.Id)
            .FirstAsync();

        var c1 = new Course("Osnove teorije muzike", "Uvod u osnove teorije muzike.", 800, new DateTime(2024, 8, 10), new DateTime(2024, 10, 10), instructorId);
        c1.SetPublishedStatus(true);

        var c2 = new Course("Napredne tehnike gitare", "Napredne tehnike i improvizacija.", 800, new DateTime(2024, 9, 12), new DateTime(2024, 10, 12), instructorId);
        c2.SetPublishedStatus(true);

        context.Set<Course>().AddRange(c1, c2);
        await context.SaveChangesAsync();
    }
}

internal static class LectureSeed
{
    public static async Task SeedLectures(ENoteContext context)
    {
        if (await context.Set<Lecture>().AnyAsync())
        {
            return;
        }

        var courses = await context.Set<Course>()
            .OrderBy(c => c.Id)
            .Take(2)
            .ToListAsync();

        if (courses.Count < 2)
        {
            return;
        }

        context.AddRange(
            new Lecture("Uvodno predavanje", "Amfiteatar gradskog BKC-a", 90, new DateTime(2024, 8, 11, 19, 30, 0), LectureType.Theoretical, null, courses[0].Id),
            new Lecture("Uvodno predavanje", "Amfiteatar gradskog BKC-a", 60, new DateTime(2024, 8, 19, 19, 30, 0), LectureType.Theoretical, null, courses[1].Id)
        );

        await context.SaveChangesAsync();
    }
}

internal static class InstrumentSeed
{
    public static async Task SeedInstruments(ENoteContext context)
    {
        if (await context.Set<Instrument>().AnyAsync())
        {
            return;
        }

        var shopId = await context.Set<MusicStore>()
            .OrderBy(s => s.Id)
            .Select(s => s.Id)
            .FirstAsync();

        // ponytail: IDs are stable - seeded via HasData with explicit values in ModelBuilderSeed
        const int stringTypeId = 1;
        const int percussionTypeId = 2;
        const int brassTypeId = 3;
        const int keysTypeId = 4;
        const int accessoriesTypeId = 5;

        Instrument[] allInstruments = [.. new[]
        {
            StringInstruments.GetInstruments(shopId, stringTypeId),
            PercussionInstruments.GetInstruments(shopId, percussionTypeId),
            BrassInstruments.GetInstruments(shopId, brassTypeId),
            KeysInstruments.GetInstruments(shopId, keysTypeId),
            AccessoriesInstruments.GetInstruments(shopId, accessoriesTypeId)
        }.SelectMany(x => x)];

        context.Set<Instrument>().AddRange(allInstruments);

        await context.SaveChangesAsync();
    }
}

internal static class StringInstruments
{
    public static Instrument[] GetInstruments(int shopId, int typeId) =>
    [
        new Instrument("Stratocaster", "Fender", "KlasiÄna elektriÄna gitara poznata po svojoj svestranosti i glatkoj svirljivosti.", "instruments/strat.webp", typeId, shopId),
        new Instrument("Les Paul", "Gibson", "Legendarna elektriÄna gitara omiljena zbog bogatog tona i odrÅ¾avanja.", "instruments/les-paul.webp", typeId, shopId),
        new Instrument("RG", "Ibanez", "Visokoperformansna elektriÄna gitara popularna meÄ‘u rok i metal sviraÄima.", "instruments/rg.webp", typeId, shopId),
        new Instrument("Custom 24", "PRS", "Visokokvalitetna elektriÄna gitara poznata po svojoj prelijepoj izradi i zvuku.", "instruments/prs.webp", typeId, shopId),
        new Instrument("Pacifica", "Yamaha", "Svestrana elektriÄna gitara pogodna za razliÄite Å¾anrove.", "instruments/pacifica.webp", typeId, shopId),
        new Instrument("Dinky", "Jackson", "ElektriÄna gitara dizajnirana za brzo sviranje i snaÅ¾an zvuk.", "instruments/dinky.webp", typeId, shopId),
        new Instrument("214ce", "Taylor", "Svestrana i lijepo izraÄ‘ena akustiÄna gitara, poznata po svom svijetlom i artikulisanom tonu.", "instruments/214ce.webp", typeId, shopId),
        new Instrument("D-28", "Martin", "IkoniÄna dreadnought gitara sa bogatom historijom, poznata po svom dubokom, rezonantnom basu i jasnim visokim tonovima.", "instruments/d-28.webp", typeId, shopId),
        new Instrument("J-45", "Gibson", "ÄŒesto nazivan \"radnim konjem\" meÄ‘u akustiÄnim gitarama, ovaj dreadnought sa zaobljenim ramenima pruÅ¾a topao, blag ton koji je savrÅ¡en za kantautore.", "instruments/j-45.webp", typeId, shopId),
        new Instrument("S6", "Seagull", "S6 proizvodi topao, bogat zvuk sa blago rustiÄnim karakterom, Å¡to je Äini omiljenom meÄ‘u muziÄarima koji sviraju folk i roots muziku.", "instruments/s6.webp", typeId, shopId),
        new Instrument("Precision Bass", "Fender", "Industrijski standard bas gitara poznata po dubokom, udarnom zvuku.", "instruments/precision.webp", typeId, shopId),
        new Instrument("Thunderbird", "Gibson", "IkoniÄna bas gitara poznata po jedinstvenom dizajnu i snaÅ¾nom zvuku.", "instruments/thunderbird.webp", typeId, shopId),
        new Instrument("StingRay", "Music Man", "Legendarna elektriÄna bas gitara, prepoznatljiva po svom moÄ‡nom, artikulisanom zvuku, elegantnom dizajnu i vrhunskoj svirljivosti.", "instruments/stingray.webp", typeId, shopId)
    ];
}

internal static class PercussionInstruments
{
    public static Instrument[] GetInstruments(int shopId, int typeId) =>
    [
        new Instrument("Export", "Pearl", "PristupaÄan bubanj set savrÅ¡en za poÄetnike i srednje napredne bubnjare.", "instruments/export.webp", typeId, shopId),
        new Instrument("Imperialstar", "Tama", "Svestran bubanj set sa izvrsnom izradom i zvukom.", "instruments/imperialstar.webp", typeId, shopId),
        new Instrument("Breakbeats", "Ludwig", "Kompaktni bubanj set dizajniran za prenosivost i odliÄan ton.", "instruments/breakbeats.webp", typeId, shopId)
    ];
}

internal static class BrassInstruments
{
    public static Instrument[] GetInstruments(int shopId, int typeId) =>
    [
        new Instrument("YAS-280", "Yamaha", "Popularni saksofon meÄ‘u studentima i srednje naprednim sviraÄima.", "instruments/yas.webp", typeId, shopId),
        new Instrument("Stradivarius", "Bach", "Profesionalni trombon poznat po bogatom tonu i preciznoj intonaciji.", "instruments/stradivarius.webp", typeId, shopId)
    ];
}

internal static class KeysInstruments
{
    public static Instrument[] GetInstruments(int shopId, int typeId) =>
    [
        new Instrument("Minilogue", "Korg", "Analogni sintisajzer poznat po svom bogatom, toplom zvuku.", "instruments/minilogue.webp", typeId, shopId),
        new Instrument("Juno-DS", "Roland", "Svestrani sintisajzer popularan za Å¾ive nastupe i studijsku upotrebu.", "instruments/juno-ds.webp", typeId, shopId)
    ];
}

internal static class AccessoriesInstruments
{
    public static Instrument[] GetInstruments(int shopId, int typeId) =>
    [
        new Instrument("Blues Junior IV", "Fender", "Kompaktno, ali snaÅ¾no cijevno pojaÄalo koje pruÅ¾a klasiÄan Fender ton sa dodanom modernom svestranoÅ¡Ä‡u.", "instruments/blues-junior.webp", typeId, shopId),
        new Instrument("DSL40CR-DS", "Marshall", "Vrlo svestrano cijevno pojaÄalo koje nudi sve, od klasiÄne rock distorzije do Å¾estokih solaÅ¾a.", "instruments/dsl40cr.webp", typeId, shopId),
        new Instrument("AC15C1", "Vox", "Poznato po svojim svijetlim Äistim tonovima i karakteristiÄnom \"Top Boost\" overdrive efektu, savrÅ¡eno je za one koji traÅ¾e vintage britanski zvuk.", "instruments/ac15c1.webp", typeId, shopId),
        new Instrument("Rocker 15", "Orange", "Idealno je za kuÄ‡ne probe i manje nastupe, nudeÄ‡i niz tonova od Äistog do prljavog sa jednostavnim i preglednim kontrolama.", "instruments/rocker-15.webp", typeId, shopId),
        new Instrument("Katana-100 MkII", "Boss", "Moderno digitalno pojaÄalo koje kombinuje veliku snagu sa nevjerovatnom svestranoÅ¡Ä‡u.", "instruments/katana-100.webp", typeId, shopId)
    ];
}

internal static class EnrollmentSeed
{
    public static async Task SeedEnrollments(ENoteContext context)
    {
        if (await context.Set<Enrollment>().AnyAsync())
        {
            return;
        }

        var studentId = await context.Set<Student>()
            .OrderBy(s => s.Id)
            .Select(s => s.Id)
            .FirstOrDefaultAsync();

        if (studentId == 0)
        {
            return;
        }

        var courseIds = await context.Set<Course>()
            .Where(c => c.IsPublished)
            .Select(c => c.Id)
            .ToListAsync();

        List<Enrollment> enrollments = [.. courseIds.Select(courseId => new Enrollment(studentId, courseId, EnrollmentStatus.Active))];

        context.Set<Enrollment>().AddRange(enrollments);
        await context.SaveChangesAsync();
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\Seed\IdentitySeed.cs`
**Hash**: `8555a1f436f5` | **Size**: 3422 chars

**Classes**: IdentitySeed, RoleSeed, StoreSeed
```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Identity.Users;
using eNote.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace eNote.Infrastructure.Data.Seed;

public static class IdentitySeed
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();

        var context = serviceProvider.GetRequiredService<ENoteContext>();

        var provisioningService = serviceProvider.GetRequiredService<IUserProvisioningService>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var defaultPassword = configuration["Seed:DefaultPassword"] ?? "Test1234!";

        await RoleSeed.SeedRoles(roleManager);

        var defaultStoreId = await StoreSeed.EnsureDefaultStoreAsync(context);

        (string, string, string, int?)[] testUsers = new[]
        {
            ("admin", "admin@enote.com", AppRoles.Administrator, (int?)null),
            ("instructor", "instructor@enote.com", AppRoles.Instructor, (int?)null),
            ("student", "student@enote.com", AppRoles.Student, (int?)null),
            ("storeemployee", "storeEmployee@enote.com", AppRoles.StoreEmployee, (int?)defaultStoreId)
        };

        foreach ((var username, var email, var role, var storeId) in testUsers)
        {
            (var _, var error) = await provisioningService.ProvisionUserAsync(new UserProvisionRequest
            {
                Username = username,
                Email = email,
                Password = defaultPassword,
                Role = role,
                MusicStoreId = storeId
            });

            if (error is not null)
            {
                throw new BusinessException(error);
            }
        }
    }
}

internal static class RoleSeed
{
    public static async Task SeedRoles(RoleManager<AppRole> roleManager)
    {
        string[] roles = [AppRoles.Administrator, AppRoles.Instructor, AppRoles.Student, AppRoles.StoreEmployee];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new AppRole { Name = role });

                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));

                    throw new BusinessException(Messages.RoleCreateFailed(role, errors));
                }
            }
        }
    }
}

internal static class StoreSeed
{
    public static async Task<int> EnsureDefaultStoreAsync(ENoteContext context)
    {
        var storeId = await context.Set<MusicStore>()
            .OrderBy(x => x.Id)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();

        if (storeId.HasValue)
        {
            return storeId.Value;
        }

        var store = new MusicStore("Test Music Store", "09:00-17:00");

        context.Set<MusicStore>().Add(store);

        await context.SaveChangesAsync();

        return store.Id;
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Data\Seed\ModelBuilderSeed.cs`
**Hash**: `d026886ebf5b` | **Size**: 1474 chars

**Classes**: ModelBuilderSeed
```cs
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Data.Seed;

internal static class ModelBuilderSeed
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        SeedAddresses(modelBuilder);
        SeedInstrumentTypes(modelBuilder);
    }

    private static void SeedAddresses(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Address>().HasData(
            new Address { Id = 1, City = "Sarajevo", Street = "Bistrik", Number = "12" },
            new Address { Id = 2, City = "Sarajevo", Street = "Maršala Tita", Number = "15" },
            new Address { Id = 3, City = "Sarajevo", Street = "Mula Mustafe Bašeskije", Number = "8" },
            new Address { Id = 4, City = "Sarajevo", Street = "Obala Kulina bana", Number = "18" },
            new Address { Id = 5, City = "Sarajevo", Street = "Veliki Alifakovac", Number = "14" }
        );
    }

    private static void SeedInstrumentTypes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InstrumentType>().HasData(
            new InstrumentType { Id = 1, Type = "Žičani", MonthlyFee = 45m },
            new InstrumentType { Id = 2, Type = "Udaraljke", MonthlyFee = 35m },
            new InstrumentType { Id = 3, Type = "Limeni", MonthlyFee = 55m },
            new InstrumentType { Id = 4, Type = "Tipke", MonthlyFee = 65m },
            new InstrumentType { Id = 5, Type = "Dodatna oprema", MonthlyFee = 15m }
        );
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Identity\AppRole.cs`
**Hash**: `1ea54f2c80bf` | **Size**: 125 chars

**Classes**: AppRole
```cs
using Microsoft.AspNetCore.Identity;

namespace eNote.Infrastructure.Identity;

public class AppRole : IdentityRole<int>
{
}

```

---

## File: `eNote\eNote.Infrastructure\Identity\AppUser.cs`
**Hash**: `ee3da42d11c5` | **Size**: 449 chars

**Classes**: AppUser
```cs
using eNote.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace eNote.Infrastructure.Identity;

public class AppUser : IdentityUser<int>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public byte[]? Picture { get; set; }
    public bool IsActive { get; set; }

    public int? AddressId { get; set; }
    public Address? Address { get; set; }
}

```

---

## File: `eNote\eNote.Infrastructure\Identity\SmtpEmailService.cs`
**Hash**: `b771433af967` | **Size**: 1598 chars

**Classes**: SmtpEmailService
```cs
using System.Net;
using System.Net.Mail;
using eNote.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace eNote.Infrastructure.Identity;

public sealed class SmtpEmailService : IEmailService
{
    private readonly string _host;
    private readonly string _from;
    private readonly int _port;
    private readonly bool _enableSsl;
    private readonly string? _username;
    private readonly string? _password;

    public SmtpEmailService(IConfiguration configuration)
    {
        _host = configuration["Smtp:Host"] ?? throw new InvalidOperationException("Smtp:Host is required.");
        _from = configuration["Smtp:From"] ?? throw new InvalidOperationException("Smtp:From is required.");
        _port = configuration.GetValue("Smtp:Port", 25);
        _enableSsl = configuration.GetValue("Smtp:EnableSsl", true);
        _username = configuration["Smtp:Username"];
        _password = configuration["Smtp:Password"];
    }

    public async Task SendPasswordResetAsync(string email, string token, CancellationToken cancellationToken = default)
    {
        using var message = new MailMessage(_from, email)
        {
            Subject = "Reset lozinke",
            Body = $"Token za reset lozinke: {token}"
        };

        using var client = new SmtpClient(_host, _port)
        {
            EnableSsl = _enableSsl
        };

        if (!string.IsNullOrWhiteSpace(_username))
        {
            client.Credentials = new NetworkCredential(_username, _password);
        }

        await client.SendMailAsync(message, cancellationToken);
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Identity\TokenRevocationService.cs`
**Hash**: `37bdf1092da5` | **Size**: 1846 chars

**Classes**: TokenRevocationService
### Key Cross-Cutting Interactions
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Domain.Entities;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Identity.Auth.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace eNote.Infrastructure.Identity;

public class TokenRevocationService(IAppDbContext context, IClock clock, IMemoryCache cache) : ITokenRevocationService
{
    private static string Key(string jti) => $"revoked:{jti}";

    public async Task RevokeAsync(string jti, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jti))
        {
            return;
        }

        var ttl = expiresAt - clock.UtcNow;

        if (ttl > TimeSpan.Zero)
        {
            cache.Set(Key(jti), true, ttl);
        }

        var exists = await context.Set<RevokedToken>()
            .AnyAsync(x => x.Jti == jti, cancellationToken);

        if (exists)
        {
            return;
        }

        context.Set<RevokedToken>().Add(new RevokedToken
        {
            Jti = jti,
            ExpiresAt = expiresAt,
            RevokedAt = clock.UtcNow
        });

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jti))
        {
            return false;
        }

        if (cache.TryGetValue(Key(jti), out _))
        {
            return true;
        }

        var revoked = await context.Set<RevokedToken>()
            .AsNoTracking()
            .AnyAsync(x => x.Jti == jti && x.ExpiresAt > clock.UtcNow, cancellationToken);

        if (revoked)
        {
            cache.Set(Key(jti), true, TimeSpan.FromHours(1));
        }

        return revoked;
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Identity\TokenService.cs`
**Hash**: `827d69cae04d` | **Size**: 1475 chars

**Classes**: TokenService
```cs
using eNote.Application.Common.Time;
using eNote.Application.Features.Identity.Auth.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace eNote.Infrastructure.Identity;

public sealed class TokenService(IConfiguration configuration, IClock clock) : ITokenService
{
    public string GenerateToken(int userId, string username, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expirationDays = configuration.GetValue<int>("Jwt:ExpirationDays", 7);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: clock.UtcNow.AddDays(expirationDays),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Identity\UserAccountService.cs`
**Hash**: `424e07efc9cd` | **Size**: 8207 chars

**Classes**: UserAccountService
```cs
using eNote.Application.Common.Localization;
using eNote.Application.Features.Identity.Users.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Identity;

public sealed class UserAccountService(UserManager<AppUser> userManager) : IUserAccountService
{
    private const int MaxPictureSizeBytes = 5 * 1024 * 1024;

    public async Task<int?> FindUserIdByUsernameAsync(string username)
    {
        var user = await userManager.FindByNameAsync(username.Trim());

        return user?.Id;
    }

    public async Task<(int? UserId, string? Error)> CreateUserAsync(string username, string email, string password, string? firstName, string? lastName)
    {
        var normalizedUsername = username.Trim();
        var normalizedEmail = email.Trim();

        if (await userManager.FindByNameAsync(normalizedUsername) is not null)
        {
            return (null, Messages.UsernameTaken);
        }

        if (await userManager.FindByEmailAsync(normalizedEmail) is not null)
        {
            return (null, Messages.EmailTaken);
        }

        var user = new AppUser
        {
            UserName = normalizedUsername,
            Email = normalizedEmail,
            EmailConfirmed = true,
            IsActive = true,
            FirstName = firstName?.Trim(),
            LastName = lastName?.Trim()
        };

        var createResult = await userManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));

            return (null, Messages.UserCreateFailed(normalizedUsername, errors));
        }

        return (user.Id, null);
    }

    public async Task<(bool Success, string? Error)> AssignSingleRoleAsync(int userId, string role)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return (false, Messages.NotFound);
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        string[] toRemove = [.. currentRoles.Where(r => r != role)];

        if (toRemove.Length > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, toRemove);

            if (!removeResult.Succeeded)
            {
                var errors = string.Join("; ", removeResult.Errors.Select(e => e.Description));
                return (false, Messages.UserRoleRemoveFailed(user.UserName!, errors));
            }
        }

        if (!currentRoles.Contains(role))
        {
            var addResult = await userManager.AddToRoleAsync(user, role);

            if (!addResult.Succeeded)
            {
                var errors = string.Join("; ", addResult.Errors.Select(e => e.Description));
                return (false, Messages.UserRoleAssignFailed(role, user.UserName!, errors));
            }
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return (false, Messages.NotFound);
        }

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (!result.Succeeded)
        {
            return (false, string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateExistingUserAsync(int userId, string email, string? firstName, string? lastName, DateTime? dateOfBirth = null)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return (false, Messages.NotFound);
        }

        var normalizedEmail = email.Trim();

        if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            var existingWithEmail = await userManager.FindByEmailAsync(normalizedEmail);

            if (existingWithEmail is not null && existingWithEmail.Id != userId)
            {
                return (false, Messages.EmailTaken);
            }

            user.Email = normalizedEmail;
            user.NormalizedEmail = userManager.NormalizeEmail(normalizedEmail);
        }

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
        }

        if (!user.IsActive)
        {
            user.IsActive = true;
        }

        user.FirstName = firstName?.Trim() ?? user.FirstName;
        user.LastName = lastName?.Trim() ?? user.LastName;

        if (dateOfBirth.HasValue)
        {
            user.DateOfBirth = dateOfBirth.Value.Date;
        }

        var updateResult = await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));

            return (false, Messages.UserUpdateFailed(user.UserName!, errors));
        }

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdatePictureAsync(int userId, byte[] picture)
    {
        if (picture.Length == 0 || picture.Length > MaxPictureSizeBytes)
        {
            return (false, Messages.FileTooLarge);
        }

        if (!IsValidImage(picture))
        {
            return (false, Messages.InvalidFileFormat);
        }

        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return (false, Messages.NotFound);
        }

        user.Picture = picture;

        var updateResult = await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            return (false, Messages.UserUpdateFailed(user.UserName!, errors));
        }

        return (true, null);
    }

    public async Task<(byte[]? Data, string? ContentType)> GetPictureAsync(int userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user?.Picture is not { Length: > 0 } picture)
        {
            return (null, null);
        }

        return (picture, DetectContentType(picture));
    }

    public async Task<(bool Success, string? Error)> DeletePictureAsync(int userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return (false, Messages.NotFound);
        }

        user.Picture = null;

        var updateResult = await userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
            return (false, Messages.UserUpdateFailed(user.UserName!, errors));
        }

        return (true, null);
    }

    public async Task<bool> IsAddressInUseAsync(int addressId) =>
        await userManager.Users.AnyAsync(u => u.AddressId == addressId);

    private static bool IsValidImage(byte[] data)
    {
        if (data.Length < 3)
        {
            return false;
        }

        var isJpeg = data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF;
        var isPng = data.Length >= 4 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47;
        var isWebp = data.Length >= 12 && data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46;

        return isJpeg || isPng || isWebp;
    }

    private static string DetectContentType(byte[] data)
    {
        if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (data.Length >= 4 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
        {
            return "image/png";
        }

        if (data.Length >= 12 && data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46)
        {
            return "image/webp";
        }

        return "application/octet-stream";
    }
}
```

---

## File: `eNote\eNote.Infrastructure\Identity\UserIdentityService.cs`
**Hash**: `d140e6bf2dc9` | **Size**: 1894 chars

**Classes**: UserIdentityService
```cs
﻿using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Identity;

public sealed class UserIdentityService(UserManager<AppUser> userManager) : IUserIdentityService
{
    public async Task<UserIdentityDto?> GetUserAsync(int userId)
    {
        var user = await userManager.Users
            .AsNoTracking()
            .Include(u => u.Address)
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user is null ? null : Map(user);
    }

    public async Task<IReadOnlyDictionary<int, UserIdentityDto>> GetUsersBulkAsync(IEnumerable<int> userIds)
    {
        HashSet<int> ids = [.. userIds];

        if (ids.Count == 0)
        {
            return new Dictionary<int, UserIdentityDto>();
        }

        var users = await userManager.Users
            .AsNoTracking()
            .Include(u => u.Address)
            .Where(u => ids.Contains(u.Id))
            .ToListAsync();

        return users.ToDictionary(u => u.Id, Map);
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(int userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        return user is null ? [] : [.. await userManager.GetRolesAsync(user)];
    }

    private static UserIdentityDto Map(AppUser user) => new()
    {
        Id = user.Id,
        Username = user.UserName!,
        FirstName = user.FirstName,
        LastName = user.LastName,
        DateOfBirth = user.DateOfBirth,
        HasPicture = user.Picture is { Length: > 0 },
        Address = user.Address is null ? null : new UserAddressDto
        {
            City = user.Address.City,
            Street = user.Address.Street,
            Number = user.Address.Number
        },
        IsActive = user.IsActive
    };
}

```

---

## File: `eNote\eNote.Infrastructure\Messaging\MassTransitServiceExtensions.cs`
**Hash**: `ea4a9593a4b0` | **Size**: 1084 chars

**Classes**: MassTransitServiceExtensions
### Key Cross-Cutting Interactions
- Uses **RabbitMQ|IEventBus|Outbox** → Async integration events (RabbitMQ)

```cs
﻿using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace eNote.Infrastructure.Messaging;

public static class MassTransitServiceExtensions
{
    public static IServiceCollection AddRabbitMqMassTransit(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureBus = null)
    {
        services.AddMassTransit(x =>
        {
            configureBus?.Invoke(x);

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(
                    RabbitMqConfiguration.GetHost(configuration),
                    RabbitMqConfiguration.GetVirtualHost(configuration),
                    h =>
                    {
                        h.Username(RabbitMqConfiguration.GetUsername(configuration));
                        h.Password(RabbitMqConfiguration.GetPassword(configuration));
                    });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Messaging\RabbitMqConfiguration.cs`
**Hash**: `37c097fe1aee` | **Size**: 965 chars

**Classes**: RabbitMqConfiguration
### Key Cross-Cutting Interactions
- Uses **RabbitMQ|IEventBus|Outbox** → Async integration events (RabbitMQ)

```cs
using Microsoft.Extensions.Configuration;

namespace eNote.Infrastructure.Messaging;

public static class RabbitMqConfiguration
{
    public static string GetHost(IConfiguration configuration) =>
        configuration["RabbitMQ:Host"] ?? "localhost";

    public static string GetVirtualHost(IConfiguration configuration) =>
        configuration["RabbitMQ:VirtualHost"] ?? "/";

    public static string GetUsername(IConfiguration configuration) =>
        configuration["RabbitMQ:Username"]
        ?? configuration["RabbitMQ:User"]
        ?? "guest";

    public static string GetPassword(IConfiguration configuration) =>
        configuration["RabbitMQ:Password"] ?? "guest";

    public static bool IsConfigured(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(configuration["RabbitMQ:Host"])
        || !string.IsNullOrWhiteSpace(configuration["RabbitMQ:User"])
        || !string.IsNullOrWhiteSpace(configuration["RabbitMQ:Username"]);
}

```

---

## File: `eNote\eNote.Infrastructure\Messaging\RentalNotificationDispatcher.cs`
**Hash**: `6674be736a5c` | **Size**: 3245 chars

**Classes**: RentalNotificationDispatcher
### Key Cross-Cutting Interactions
- Uses **RabbitMQ|IEventBus|Outbox** → Async integration events (RabbitMQ)
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Domain.Entities;
using System.Text.Json;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;
using eNote.Contracts.Rentals;

namespace eNote.Infrastructure.Messaging;

public sealed class RentalNotificationDispatcher(
    IAppDbContext context,
    IClock clock) : IRentalNotificationDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task DispatchCreatedAsync(InstrumentRentalDto rental, int studentUserId, CancellationToken cancellationToken = default)
    {
        var message = new RentalStatusChanged(rental.Id, studentUserId, studentUserId, rental.RentalStatus.ToString(), rental.InstrumentModel, "Zahtjev za iznajmljivanje poslan", $"Vaš zahtjev za instrument {rental.InstrumentModel} je poslan prodavnici {rental.StoreName} i čeka odobrenje.", clock.UtcNow);

        EnqueueOutbox(message);
        return Task.CompletedTask;
    }

    public Task DispatchTransitionAsync(InstrumentRentalDto rental, RentalTrigger trigger, int actorUserId, CancellationToken cancellationToken = default)
    {
        (var title, var body) = BuildNotificationContent(rental, trigger);

        var message = new RentalStatusChanged(rental.Id, rental.StudentUserId, actorUserId, rental.RentalStatus.ToString(), rental.InstrumentModel, title, body, clock.UtcNow);

        EnqueueOutbox(message);
        return Task.CompletedTask;
    }

    private void EnqueueOutbox(RentalStatusChanged message)
    {
        var entry = new RentalNotificationOutbox
        {
            PayloadJson = JsonSerializer.Serialize(message, JsonOptions)
        };

        context.Set<RentalNotificationOutbox>().Add(entry);
    }

    private static (string Title, string Body) BuildNotificationContent(InstrumentRentalDto rental, RentalTrigger trigger) =>
        trigger switch
        {
            RentalTrigger.Approve =>
                ("Zahtjev odobren", $"Vaš zahtjev za instrument {rental.InstrumentModel} je odobren. Mjesečna naknada: {rental.Fee:F2} KM."),
            RentalTrigger.Reject =>
                ("Zahtjev odbijen", string.IsNullOrWhiteSpace(rental.Note) ? $"Vaš zahtjev za instrument {rental.InstrumentModel} je odbijen." : $"Vaš zahtjev za instrument {rental.InstrumentModel} je odbijen. Razlog: {rental.Note}"),
            RentalTrigger.Pickup =>
                ("Instrument preuzet", $"Preuzeli ste instrument {rental.InstrumentModel}."),
            RentalTrigger.Complete =>
                ("Iznajmljivanje završeno", $"Iznajmljivanje instrumenta {rental.InstrumentModel} je uspješno završeno."),
            RentalTrigger.Cancel =>
                ("Zahtjev otkazan", $"Vaš zahtjev za instrument {rental.InstrumentModel} je otkazan."),
            RentalTrigger.ReturnEarly =>
                ("Instrument vraćen prije roka", $"Instrument {rental.InstrumentModel} je vraćen prije planiranog roka."),
            _ => ("Status iznajmljivanja promijenjen", $"Status iznajmljivanja za instrument {rental.InstrumentModel} je ažuriran.")
        };
}
```

---

## File: `eNote\eNote.Infrastructure\Messaging\RentalNotificationOutboxPublisher.cs`
**Hash**: `0bc3c5b953f9` | **Size**: 2447 chars

**Classes**: RentalNotificationOutboxPublisher
### Key Cross-Cutting Interactions
- Uses **RabbitMQ|IEventBus|Outbox** → Async integration events (RabbitMQ)
- Uses **IAppDbContext|DbContext** → Persistence boundary

```cs
using eNote.Domain.Entities;
using System.Text.Json;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Contracts.Rentals;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace eNote.Infrastructure.Messaging;

public sealed class RentalNotificationOutboxPublisher(IServiceProvider services, ILogger<RentalNotificationOutboxPublisher> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await ProcessBatchAsync(stoppingToken);
        }
        catch (OperationCanceledException) { }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var messages = await db.Set<RentalNotificationOutbox>()
            .Where(x => x.PublishedAt == null && x.Attempts < 5)
            .OrderBy(x => x.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        if (messages.Count == 0) return;

        await using var tx = await db.BeginTransactionAsync(ct);

        foreach (var message in messages)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<RentalStatusChanged>(message.PayloadJson, JsonOptions)!;
                await publisher.Publish(payload, ct);
                message.PublishedAt = clock.UtcNow;
            }
            catch (Exception ex)
            {
                message.Attempts++;
                message.LastError = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                logger.LogError(ex, "Failed to publish outbox message {Id}", message.Id);
            }
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}

```

---

## File: `eNote\eNote.Infrastructure\Storage\LocalFileStorageService.cs`
**Hash**: `11e911516bee` | **Size**: 4287 chars

**Classes**: LocalFileStorageService
```cs
using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using Microsoft.AspNetCore.Hosting;

namespace eNote.Infrastructure.Storage;

public sealed class LocalFileStorageService(IWebHostEnvironment env) : IFileStorageService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedImageContentTypes = ["image/jpeg", "image/png", "image/webp"];
    private static readonly string[] AllowedAssignmentContentTypes = ["application/pdf", "image/jpeg", "image/png"];

    public async Task<string> SaveAsync(Stream stream, string fileName, string contentType, string subfolder, CancellationToken ct = default)
    {
        if (stream.CanSeek && stream.Length > MaxFileSizeBytes)
        {
            throw new BusinessException(Messages.FileTooLarge);
        }

        await ValidateImageMagicBytesAsync(stream);

        if (!AllowedImageContentTypes.Contains(contentType.ToLowerInvariant()))
        {
            throw new BusinessException(Messages.InvalidFileFormat);
        }

        return await SaveToDiskAsync(stream, fileName, contentType, subfolder, ct);
    }

    public async Task<string> SaveAssignmentAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        if (stream.CanSeek && stream.Length > MaxFileSizeBytes)
        {
            throw new BusinessException(Messages.FileTooLarge);
        }

        await ValidateAssignmentMagicBytesAsync(stream);

        if (!AllowedAssignmentContentTypes.Contains(contentType.ToLowerInvariant()))
        {
            throw new BusinessException(Messages.InvalidFileFormat);
        }

        return await SaveToDiskAsync(stream, fileName, contentType, "assignments", ct);
    }

    private async Task<string> SaveToDiskAsync(Stream stream, string fileName, string contentType, string subfolder, CancellationToken ct)
    {
        var uploadsRoot = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads", subfolder);
        Directory.CreateDirectory(uploadsRoot);

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext))
        {
            ext = contentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                "application/pdf" => ".pdf",
                _ => ".bin"
            };
        }

        var uniqueName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploadsRoot, uniqueName);

        stream.Position = 0;
        await using FileStream fileStream = File.Create(fullPath);
        await stream.CopyToAsync(fileStream, ct);

        return $"/api/uploads/{subfolder}/{uniqueName}";
    }

    private static async Task ValidateImageMagicBytesAsync(Stream stream)
    {
        var header = new byte[4];
        var read = await stream.ReadAsync(header.AsMemory(0, 4));

        if (read < 3)
        {
            throw new BusinessException(Messages.InvalidFileFormat);
        }

        var isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
        var isRiff = header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46;

        if (!isJpeg && !isPng && !isRiff)
        {
            throw new BusinessException(Messages.InvalidFileFormat);
        }
    }

    private static async Task ValidateAssignmentMagicBytesAsync(Stream stream)
    {
        var header = new byte[4];
        var read = await stream.ReadAsync(header.AsMemory(0, 4));

        if (read < 3)
        {
            throw new BusinessException(Messages.InvalidFileFormat);
        }

        var isPdf = header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46;
        var isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;

        if (!isPdf && !isJpeg && !isPng)
        {
            throw new BusinessException(Messages.InvalidFileFormat);
        }
    }
}

```

---

