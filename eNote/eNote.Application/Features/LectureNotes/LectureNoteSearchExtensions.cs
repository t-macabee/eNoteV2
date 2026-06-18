using eNote.Domain.Entities;

namespace eNote.Application.Features.LectureNotes;

public static class LectureNoteSearchExtensions
{
    public static IQueryable<LectureNote> ApplySearch(this IQueryable<LectureNote> query, LectureNoteSearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Title))
        {
            query = query.Where(x => x.Title.Contains(search.Title));
        }

        return query;
    }
}
