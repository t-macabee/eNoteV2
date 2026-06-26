using eNote.Application.Features.Rentals.ReferenceData;

namespace eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;

public interface IInstrumentTypeService : IReferenceCrudService<InstrumentTypeDto, InstrumentTypeRequest, InstrumentTypeSearchObject>
{
}
