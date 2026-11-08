using eNote.API.Controllers.Base;
using eNote.API.Extensions;
using eNote.Application.Common.Files;
using eNote.Application.Constants;
using eNote.Application.Features.Rentals.ReferenceData.MusicStores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace eNote.API.Controllers.Rentals;

[Authorize(Roles = AppRoles.StoreEmployee)]
[Route("api/v{version:apiVersion}/shop/store")]
public sealed class ShopStoreController(MusicStoreService storeService) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(MusicStoreDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MusicStoreDto>> GetOwnStore(CancellationToken ct)
    {
        return Ok(await storeService.GetOwnStoreAsync(ct));
    }

    [HttpPut]
    [ProducesResponseType(typeof(MusicStoreDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MusicStoreDto>> UpdateOwnStore([FromBody] ShopStoreUpdateRequest request, CancellationToken ct)
    {
        return Ok(await storeService.UpdateOwnStoreAsync(request, ct));
    }

    [HttpPost("image")]
    [RequestSizeLimit(FileUploadLimits.MaxRequestBytes)]
    [EnableRateLimiting(RateLimitingExtensions.UploadsPolicy)]
    [ProducesResponseType(typeof(MusicStoreDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MusicStoreDto>> UploadOwnStoreImage(IFormFile? file, CancellationToken ct) =>
        await UploadAsync(file, (stream, contentType, token) => storeService.UploadOwnStoreImageAsync(stream, contentType, token), ct);
}
