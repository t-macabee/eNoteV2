using eNote.Application.Common.Services.Interfaces;
using eNote.Application.Features.Instruments.DTOs;
using eNote.Application.Features.Instruments.Requests;
using eNote.Application.Features.Instruments.Search;

namespace eNote.Application.Features.Instruments.Services.Interfaces
{
    public interface IInstrumentService : ICRUDService<InstrumentDto, InstrumentSearchObject, InstrumentCreateRequest, InstrumentUpdateRequest> { }
}
