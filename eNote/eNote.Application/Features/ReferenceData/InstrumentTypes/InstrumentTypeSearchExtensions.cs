using eNote.Application.Common.Search;
using eNote.Domain.Entities;

namespace eNote.Application.Features.ReferenceData.InstrumentTypes;

public static class InstrumentTypeSearchExtensions
{
    public static IQueryable<InstrumentType> ApplySearch(this IQueryable<InstrumentType> query, InstrumentTypeSearchObject search) =>
        query.WhereContainsIf(search.Type, x => x.Type.Contains(search.Type!));
}