using eNote.Application.DTOs;
using eNote.Application.Interfaces;
using eNote.Application.Interfaces.Ports;
using eNote.Application.Requests.Instruments;
using eNote.Application.SearchObjects;
using eNote.Application.Services.Base;
using eNote.Domain.Entities;
using eNote.Domain.Entities.Users;
using eNote.Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Services
{
    public class InstrumentService(IAppDbContext context, IMapper mapper)
        : CRUDService<InstrumentDto, InstrumentSearchObject, InstrumentInsertRequest, InstrumentUpdateRequest, Instrument>(context, mapper), IInstrumentService
    {
        protected override IQueryable<Instrument> AddIncludes(IQueryable<Instrument> query)
        {
            return query                
                .Include(x => x.MusicShop)
                .Include(x => x.InstrumentType)
                .Include(x => x.InstrumentRentals);
        }

        protected override IQueryable<Instrument> AddFilter(InstrumentSearchObject search, IQueryable<Instrument> query)
        {
            query = query.Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(search.Model))            
                query = query.Where(x => x.Model.Contains(search.Model));            

            if (!string.IsNullOrWhiteSpace(search.Manufacturer))            
                query = query.Where(x => x.Manufacturer.Contains(search.Manufacturer));            

            if (search.InstrumentTypeId.HasValue)            
                query = query.Where(x => x.InstrumentTypeId == search.InstrumentTypeId);

            if (search.MusicShopId.HasValue)
                query = query.Where(x => x.MusicShopId == search.MusicShopId);

            if (search.IsAvailable.HasValue)
            {
                if (search.IsAvailable.Value)
                    query = query.Where(x => !x.InstrumentRentals.Any(x => x.RentalStatus == InstrumentRentalStatus.Approved));
                else
                    query = query.Where(x => x.InstrumentRentals.Any(x => x.RentalStatus == InstrumentRentalStatus.Approved));                
            }

            return query;
        }        

        protected override async Task BeforeInsertAsync(InstrumentInsertRequest request, Instrument entity)
        {
            var existingType = await _context.Set<InstrumentType>()
                .AnyAsync(x => x.Id == request.InstrumentTypeId);

            if (!existingType)
                throw new InvalidOperationException("Vrsta instrumenta ne postoji.");

            var existingShop = await _context.Set<MusicShop>()
                .AnyAsync(x => x.Id == request.MusicShopId);

            if (!existingShop)
                throw new InvalidOperationException("Music shop ne postoji.");

            await base.BeforeInsertAsync(request, entity);
        }        

        public override async Task DeleteAsync(int id)
        {
            var instrument = await _context.Set<Instrument>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new KeyNotFoundException("ID nije pronađen.");

            var approvedRental = await _context.Set<InstrumentRental>()
                .AnyAsync(x => x.InstrumentId == id && x.RentalStatus == InstrumentRentalStatus.Approved);

            if (approvedRental)
                throw new InvalidOperationException("Instrument se ne može obrisati jer je trenutno iznajmljen.");

            instrument.IsActive = false;

            await _context.SaveChangesAsync();
        }

        protected override async Task<Instrument> AfterSaveAsync(Instrument entity)
        {
            return await AddIncludes(_context.Set<Instrument>().AsNoTracking())
                .FirstAsync(x => x.Id == entity.Id);
        }

        protected override IQueryable<Instrument> AddIdFilter(IQueryable<Instrument> query) => query.Where(x => x.IsActive);
    }
}
