using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Queryable;
using eNote.Application.Features.Instruments.Search;
using eNote.Application.Features.Instruments.Services.Interfaces;
using eNote.Application.Features.MusicStores.Services.Interfaces;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Instruments.Services
{
    public class InstrumentService(IAppDbContext context, IMapper mapper, IMusicStoreContextService storeContext) : IInstrumentService
    {
        private readonly IAppDbContext _context = context;
        private readonly IMapper _mapper = mapper;
        private readonly IMusicStoreContextService _storeContext = storeContext;

        private InstrumentDto MapEntityToModel(Instrument entity) => _mapper.Map<InstrumentDto>(entity);

        public async Task<InstrumentDto> GetByIdAsync(int id, int employeeAppUserId)
        {
            var storeId = await _storeContext.GetActiveStoreAsync(employeeAppUserId);

            var query = AddIncludes(_context.Set<Instrument>()
                .AsNoTracking()
                .Where(x => x.MusicStoreId == storeId));

            query = AddIdFilter(query);

            var entity = await query.
                FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new NotFoundException("ID nije pronađen.");

            return MapEntityToModel(entity);
        }

        public async Task<InstrumentDto> GetPublicByIdAsync(int id)
        {
            var entity = await AddIncludes(_context.Set<Instrument>().AsNoTracking())
                .Where(x => x.IsActive)
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new NotFoundException("ID nije pronađen.");

            return MapEntityToModel(entity);
        }

        public async Task<PagedResult<InstrumentDto>> GetPagedAsync(InstrumentSearchObject search, int employeeAppUserId)
        {
            var storeId = await _storeContext.GetActiveStoreAsync(employeeAppUserId);

            var query = _context.Set<Instrument>()
                .AsNoTracking()
                .Where(x => x.MusicStoreId == storeId);

            query = AddIncludes(query);
            query = AddFilter(search, query);

            return await query.ToPagedResultAsync(search.Page, search.PageSize, search.IncludeTotalCount, MapEntityToModel);
        }

        public async Task<PagedResult<InstrumentDto>> GetPublicPagedAsync(InstrumentSearchObject search)
        {
            var query = _context.Set<Instrument>().AsNoTracking();

            query = AddIncludes(query);            
            query = query.Where(x => x.IsActive);

            if (search.IsAvailable.HasValue && search.IsAvailable.Value)
            {
                query = query.Where(x => !x.InstrumentRentals.Any(r =>
                    r.RentalStatus == InstrumentRentalStatus.Approved ||
                    r.RentalStatus == InstrumentRentalStatus.Active));
            }

            query = AddFilter(search, query);

            return await query.ToPagedResultAsync(search.Page, search.PageSize, search.IncludeTotalCount, MapEntityToModel);
        }

        public async Task<InstrumentDto> InsertAsync(InstrumentCreateRequest request, int employeeAppUserId)
        {
            var storeId = await _storeContext.GetActiveStoreAsync(employeeAppUserId);
            var entity = _mapper.Map<Instrument>(request);

            entity.MusicStoreId = storeId;
            await BeforeInsertAsync(request, entity);

            _context.Set<Instrument>().Add(entity);
            await _context.SaveChangesAsync();

            entity = await AfterSaveAsync(entity);

            return MapEntityToModel(entity);
        }

        public async Task<InstrumentDto> UpdateAsync(int id, InstrumentUpdateRequest request, int employeeAppUserId)
        {
            var storeId = await _storeContext.GetActiveStoreAsync(employeeAppUserId);

            var entity = await _context.Set<Instrument>().FirstOrDefaultAsync(x => x.Id == id && x.MusicStoreId == storeId) ?? throw new eNote.Application.Common.Exceptions.NotFoundException("ID nije pronađen.");

            _mapper.Map(request, entity);

            await BeforeUpdateAsync(request, entity);
            await _context.SaveChangesAsync();

            entity = await AfterSaveAsync(entity);

            return MapEntityToModel(entity);
        }

        public async Task DeleteAsync(int id, int employeeAppUserId)
        {
            var storeId = await _storeContext.GetActiveStoreAsync(employeeAppUserId);

            var instrument = await _context.Set<Instrument>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id && x.MusicStoreId == storeId)
                ?? throw new eNote.Application.Common.Exceptions.NotFoundException("ID nije pronađen.");

            var hasBlockingRental = await _context.Set<InstrumentRental>()
                .AnyAsync(r => r.InstrumentId == id &&
                               (r.RentalStatus == InstrumentRentalStatus.Approved ||
                                r.RentalStatus == InstrumentRentalStatus.Active));

            if (hasBlockingRental)
                throw new eNote.Application.Common.Exceptions.BusinessException("Instrument se ne može obrisati jer je trenutno rezervisan ili iznajmljen.");

            instrument.IsActive = false;

            await _context.SaveChangesAsync();
        }

        private IQueryable<Instrument> AddIncludes(IQueryable<Instrument> query) => query.WithInstrumentDetails();

        private IQueryable<Instrument> AddFilter(InstrumentSearchObject search, IQueryable<Instrument> query)
        {
            query = query.Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(search.Model))
                query = query.Where(x => x.Model.Contains(search.Model));

            if (!string.IsNullOrWhiteSpace(search.Manufacturer))
                query = query.Where(x => x.Manufacturer.Contains(search.Manufacturer));

            if (search.InstrumentTypeId.HasValue)
                query = query.Where(x => x.InstrumentTypeId == search.InstrumentTypeId);

            if (search.IsAvailable.HasValue)
            {
                if (search.IsAvailable.Value)
                    query = query.Where(x => !x.InstrumentRentals.Any(x =>
                        x.RentalStatus == InstrumentRentalStatus.Approved ||
                        x.RentalStatus == InstrumentRentalStatus.Active));
                else
                    query = query.Where(x => x.InstrumentRentals.Any(x =>
                        x.RentalStatus == InstrumentRentalStatus.Approved ||
                        x.RentalStatus == InstrumentRentalStatus.Active));
            }

            return query;
        }

        private Task BeforeUpdateAsync(InstrumentUpdateRequest request, Instrument entity) => Task.CompletedTask;

        private async Task BeforeInsertAsync(InstrumentCreateRequest request, Instrument entity)
        {
            var existingType = await _context.Set<InstrumentType>()
                .AnyAsync(x => x.Id == request.InstrumentTypeId);

            if (!existingType)
                throw new eNote.Application.Common.Exceptions.BusinessException("Vrsta instrumenta ne postoji.");
        }

        private async Task<Instrument> AfterSaveAsync(Instrument entity)
        {
            return await _context.Set<Instrument>()
                .AsNoTracking()
                .WithInstrumentDetails()
                .FirstAsync(x => x.Id == entity.Id);
        }

        private IQueryable<Instrument> AddIdFilter(IQueryable<Instrument> query) => query.Where(x => x.IsActive);
    }
}
