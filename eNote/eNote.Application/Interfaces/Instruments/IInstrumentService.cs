using eNote.Application.DTOs;
using eNote.Application.Interfaces.Base;
using eNote.Application.Requests.Instruments;
using eNote.Application.SearchObjects;

namespace eNote.Application.Interfaces.Instruments
{
    public interface IInstrumentService : ICRUDService<InstrumentDto, InstrumentSearchObject, InstrumentCreateRequest, InstrumentUpdateRequest> { }
}
