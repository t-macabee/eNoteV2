using eNote.API.Controllers.Base;
using eNote.Application.Features.Instruments.DTOs;
using eNote.Application.Features.Instruments.Requests;
using eNote.Application.Features.Instruments.Search;
using eNote.Application.Features.Instruments.Services.Interfaces;

namespace eNote.API.Controllers.Instruments
{
    public class InstrumentController(IInstrumentService instrumentService)
        : CRUDController<InstrumentDto, InstrumentSearchObject, InstrumentCreateRequest, InstrumentUpdateRequest>(instrumentService)
    { }
}
