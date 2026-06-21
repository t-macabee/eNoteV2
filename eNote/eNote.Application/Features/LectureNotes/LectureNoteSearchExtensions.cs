using eNote.Application.Common.Search;
using eNote.Domain.Entities;

namespace eNote.Application.Features.LectureNotes;

public static class LectureNoteSearchExtensions
{
    public static IQueryable<LectureNote> ApplySearch(this IQueryable<LectureNote> query, LectureNoteSearchObject search) =>
        query.WhereContainsIf(search.Title, x => x.Title.Contains(search.Title!));
}