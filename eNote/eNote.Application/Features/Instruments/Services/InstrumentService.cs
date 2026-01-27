using eNote.Application.Common.Persistence;
using eNote.Application.Common.Queryable;
using eNote.Application.Common.Services;
using eNote.Application.Features.Instruments.DTOs;
using eNote.Application.Features.Instruments.Requests;
using eNote.Application.Features.Instruments.Search;
using eNote.Application.Features.Instruments.Services.Interfaces;
using eNote.Domain.Entities;
using eNote.Domain.Entities.Users;
using eNote.Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Instruments.Services
{
    public class InstrumentService(IAppDbContext context, IMapper mapper)
        : CRUDService<InstrumentDto, InstrumentSearchObject, InstrumentCreateRequest, InstrumentUpdateRequest, Instrument>(context, mapper), IInstrumentService
    {
        protected override IQueryable<Instrument> AddIncludes(IQueryable<Instrument> query)
        {
            return query.WithInstrumentDetails();                
               
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

        protected override async Task BeforeInsertAsync(InstrumentCreateRequest request, Instrument entity)
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

            var hasBlockingRental = await _context.Set<InstrumentRental>()
                .AnyAsync(r => r.InstrumentId == id &&
                (r.RentalStatus == InstrumentRentalStatus.Approved ||
                r.RentalStatus == InstrumentRentalStatus.Active));

            if (hasBlockingRental)
                throw new InvalidOperationException("Instrument se ne može obrisati jer je trenutno rezervisan ili iznajmljen.");

            instrument.IsActive = false;

            await _context.SaveChangesAsync();
        }

        protected override async Task<Instrument> AfterSaveAsync(Instrument entity)
        {
            return await _context.Set<Instrument>()
                .AsNoTracking()
                .WithInstrumentDetails()
                .FirstAsync(x => x.Id == entity.Id);
        }

        protected override IQueryable<Instrument> AddIdFilter(IQueryable<Instrument> query) 
            => query.Where(x => x.IsActive);
    }
}
