using eNote.Application.DTOs;
using eNote.Application.Interfaces;
using eNote.Application.Requests.Instruments;
using eNote.Application.SearchObjects;

namespace eNote.API.Controllers
{
    public class InstrumentController(IInstrumentService instrumentService) : CRUDController<InstrumentDto, InstrumentSearchObject, InstrumentInsertRequest, InstrumentUpdateRequest>(instrumentService) { }
}
