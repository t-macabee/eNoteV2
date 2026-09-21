using eNote.API.Controllers.Base;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Employees;
using eNote.Application.Features.Rentals.ReferenceData.Addresses;
using eNote.Application.Features.Rentals.ReferenceData.MusicStores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Shop;

[Authorize(Roles = AppRoles.StoreEmployee)]
[Route("api/v{version:apiVersion}/shop/store")]
public sealed class ShopStoreController(
    MusicStoreService storeService,
    ShopEmployeeService employeeService,
    AddressService addressService,
    IStoreContext stores) : CoreController
{
    [HttpGet]
    [ProducesResponseType(typeof(MusicStoreDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MusicStoreDto>> GetOwnStore(CancellationToken ct)
    {
        var storeId = await stores.GetCurrentStoreIdAsync(ct);
        return Ok(await storeService.GetByIdAsync(storeId, ct));
    }

    [HttpPut]
    [ProducesResponseType(typeof(MusicStoreDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MusicStoreDto>> UpdateOwnStore([FromBody] ShopStoreUpdateRequest request, CancellationToken ct)
    {
        var storeId = await employeeService.GetCurrentManagerStoreIdAsync(ct);
        return Ok(await storeService.UpdateOperationalDetailsAsync(storeId, request.BusinessHours, request.PhoneNumber, ct));
    }

    [HttpPost("image")]
    [ProducesResponseType(typeof(MusicStoreDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MusicStoreDto>> UploadOwnStoreImage(IFormFile? file, CancellationToken ct) =>
        await UploadAsync(file, async (stream, fileName, contentType, token) =>
        {
            var storeId = await employeeService.GetCurrentManagerStoreIdAsync(token);
            return await storeService.UploadImageAsync(storeId, stream, fileName, contentType, token);
        }, ct);

    // Read-only address lookup for the "Uredi prodavnicu" address dropdown —
    // AdminAddressController owns address CRUD, this mirrors the same
    // read-only pattern InstrumentController uses for shop/instrument-types.
    [HttpGet("~/api/v{version:apiVersion}/shop/addresses")]
    [ProducesResponseType(typeof(PagedResult<AddressReferenceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AddressReferenceDto>>> GetAddresses(
        [FromQuery] AddressSearchObject search, CancellationToken cancellationToken)
    {
        return Ok(await addressService.GetPagedAsync(search, cancellationToken));
    }
}
