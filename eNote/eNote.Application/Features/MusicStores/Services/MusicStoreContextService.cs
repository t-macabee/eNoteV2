using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.MusicStores.Services;

public sealed class MusicStoreContextService(IAppDbContext context) : IMusicStoreContextService
{
    public async Task<int> GetActiveStoreAsync(int appUserId, CancellationToken ct = default)
    {
        var activeStore = await context.Set<MusicStoreEmployee>()
            .AsNoTracking()
            .Where(x => x.AppUserId == appUserId && x.IsActive)
            .Select(x => x.MusicStoreId)
            .SingleOrDefaultAsync(ct);

        if (activeStore == 0)
        {
            throw new BusinessException(Messages.ActiveEmployeeStoreNotFound);
        }

        return activeStore;
    }
}
