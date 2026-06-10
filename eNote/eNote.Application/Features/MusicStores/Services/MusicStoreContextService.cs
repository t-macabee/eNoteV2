using eNote.Application.Common.Persistence;
using eNote.Application.Features.MusicStores.Services.Interfaces;
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.MusicStores.Services
{
    public sealed class MusicStoreContextService(IAppDbContext context) : IMusicStoreContextService
    {
        private readonly IAppDbContext _context = context;

        public async Task<int> GetActiveStoreAsync(int appUserId, CancellationToken ct = default)
        {
            var activeStore = await _context.Set<MusicStoreEmployee>()
                .AsNoTracking()
                .Where(x => x.AppUserId == appUserId && x.IsActive)
                .Select(x => x.MusicStoreId)
                .SingleOrDefaultAsync(ct);

            if (activeStore == 0)
                throw new eNote.Application.Common.Exceptions.BusinessException("Profil uposlenika radnje nije pronađen ili nije aktivan.");

            return activeStore;
        }
    }
}
