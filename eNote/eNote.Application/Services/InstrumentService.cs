using eNote.Application.DTOs;
using eNote.Application.Interfaces;
using eNote.Application.Interfaces.Ports;
using eNote.Application.Requests.Instruments;
using eNote.Application.SearchObjects;
using eNote.Application.Services.Base;
using eNote.Domain.Entities;
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
            return query.Include(x => x.MusicShop).Include(x => x.InstrumentType);
        }

        protected override IQueryable<Instrument> AddFilter(InstrumentSearchObject search, IQueryable<Instrument> query)
        {
            if (!string.IsNullOrWhiteSpace(search.Model))            
                query = query.Where(x => x.Model.Contains(search.Model));            

            if (!string.IsNullOrWhiteSpace(search.Manufacturer))            
                query = query.Where(x => x.Model.Contains(search.Manufacturer));            

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
    }
}
