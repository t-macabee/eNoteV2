using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Identity.Employees;

public sealed class ShopEmployeeService(
    IAppDbContext context,
    IUserIdentityService identityService,
    ICurrentUserContext currentUser,
    IUserProvisioningService provisioningService)
{
    public async Task<PagedResult<ShopEmployeeDto>> GetPagedForCurrentStoreAsync(
        ShopEmployeeSearchObject search,
        CancellationToken cancellationToken = default)
    {
        var currentEmployee = await UserProfileLookup.GetActiveEmployeeAsync(context, currentUser.UserId, cancellationToken);

        var storeId = currentEmployee.MusicStoreId;

        var employees = await context.Set<MusicStoreEmployee>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(x => x.MusicStore)
            .Where(x => x.MusicStoreId == storeId && x.AppUserId != currentEmployee.AppUserId)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var users = await identityService.GetUsersBulkAsync(
            employees.Select(x => x.AppUserId),
            cancellationToken);

        List<ShopEmployeeDto> mapped = [.. employees.Select(x => Map(x, users.GetValueOrDefault(x.AppUserId)))];

        return mapped.FilterAndPage(search, search.Name, search.IsActive);
    }

    public async Task<PagedResult<ShopEmployeeDto>> GetPagedAsync(
        ShopEmployeeSearchObject search,
        CancellationToken cancellationToken = default)
    {
        IQueryable<MusicStoreEmployee> query = context.Set<MusicStoreEmployee>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(x => x.MusicStore)
            .OrderBy(x => x.Id);

        if (search.MusicStoreId.HasValue)
        {
            query = query.Where(x => x.MusicStoreId == search.MusicStoreId.Value);
        }

        List<MusicStoreEmployee> employees = await query.ToListAsync(cancellationToken);

        var users = await identityService.GetUsersBulkAsync(
            employees.Select(x => x.AppUserId),
            cancellationToken);

        List<ShopEmployeeDto> mapped = [.. employees.Select(x => Map(x, users.GetValueOrDefault(x.AppUserId)))];

        return mapped.FilterAndPage(search, search.Name, search.IsActive);
    }

    public async Task<ShopEmployeeDto> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        MusicStoreEmployee entity = await context.Set<MusicStoreEmployee>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(x => x.MusicStore)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException(Messages.EmployeeProfileNotFound);

        UserIdentityDto? user = await identityService.GetUserAsync(entity.AppUserId, cancellationToken);

        return Map(entity, user);
    }

    public async Task<(bool Success, string? Error)> SetEmployeeActiveByManagerAsync(int targetUserId, bool isActive, CancellationToken ct = default)
    {
        var currentEmployee = await UserProfileLookup.EnsureManagerAsync(context, currentUser.UserId, ct);

        var target = await context.Set<MusicStoreEmployee>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.AppUserId == targetUserId, ct)
            ?? throw new NotFoundException(Messages.EmployeeProfileNotFound);

        if (target.MusicStoreId != currentEmployee.MusicStoreId)
        {
            throw new NotFoundException(Messages.NotFound);
        }

        if (target.IsManager && target.AppUserId != currentEmployee.AppUserId)
        {
            throw new BusinessException("Store managers cannot deactivate other store managers.");
        }

        return await provisioningService.SetUserActiveAsync(targetUserId, isActive, ct);
    }

    public async Task<int> GetCurrentManagerStoreIdAsync(CancellationToken ct = default)
    {
        var currentEmployee = await UserProfileLookup.EnsureManagerAsync(context, currentUser.UserId, ct);
        return currentEmployee.MusicStoreId;
    }

    internal static ShopEmployeeDto Map(MusicStoreEmployee entity, UserIdentityDto? user) => new()
    {
        Id = entity.Id,
        AppUserId = entity.AppUserId,
        MusicStoreId = entity.MusicStoreId,
        StoreName = entity.MusicStore?.StoreName,
        FirstName = user?.FirstName,
        LastName = user?.LastName,
        Username = user?.Username,
        IsManager = entity.IsManager,
        IsActive = entity.IsActive && (user?.IsActive ?? true)
    };
}
