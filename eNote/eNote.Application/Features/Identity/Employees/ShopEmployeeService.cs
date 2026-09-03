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
    ICurrentUserContext currentUser)
{
    public async Task<PagedResult<ShopEmployeeDto>> GetPagedForCurrentStoreAsync(
        ShopEmployeeSearchObject search,
        CancellationToken cancellationToken = default)
    {
        var currentEmployee = await context.Set<MusicStoreEmployee>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.AppUserId == currentUser.UserId && x.IsActive, cancellationToken)
            ?? throw new BusinessException(Messages.EmployeeProfileNotFound);

        var storeId = currentEmployee.MusicStoreId;

        var employees = await context.Set<MusicStoreEmployee>()
            .IgnoreQueryFilters()
            .Where(x => x.MusicStoreId == storeId && x.IsActive)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var users = await identityService.GetUsersBulkAsync(
            employees.Select(x => x.AppUserId),
            cancellationToken);

        List<ShopEmployeeDto> filtered = [.. employees
            .Select(x => Map(x, users.GetValueOrDefault(x.AppUserId)))
            .Where(x => MatchesName(x, search.Name))];

        (var page, var pageSize) = PagingLimits.Normalize(search.Page, search.PageSize);

        return new PagedResult<ShopEmployeeDto>
        {
            Items = [.. filtered.Skip((page - 1) * pageSize).Take(pageSize)],
            Page = page,
            PageSize = pageSize,
            TotalCount = search.IncludeTotalCount ? filtered.Count : null
        };
    }

    private static ShopEmployeeDto Map(MusicStoreEmployee entity, UserIdentityDto? user) => new()
    {
        Id = entity.Id,
        AppUserId = entity.AppUserId,
        MusicStoreId = entity.MusicStoreId,
        FirstName = user?.FirstName,
        LastName = user?.LastName,
        Username = user?.Username,
        IsManager = entity.IsManager,
        IsActive = entity.IsActive
    };

    private static bool MatchesName(ShopEmployeeDto dto, string? name)
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

    private static bool Contains(string? value, string name) =>
        value?.Contains(name, StringComparison.OrdinalIgnoreCase) == true;
}
