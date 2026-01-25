using eNote.API.Controllers.Base;
using eNote.Application.DTOs;
using eNote.Application.Interfaces.Instruments;
using eNote.Application.Requests.Instruments;
using eNote.Application.SearchObjects;

namespace eNote.API.Controllers
{
    public class InstrumentController(IInstrumentService instrumentService) 
        : CRUDController<InstrumentDto, InstrumentSearchObject, InstrumentCreateRequest, InstrumentUpdateRequest>(instrumentService) { }
}
