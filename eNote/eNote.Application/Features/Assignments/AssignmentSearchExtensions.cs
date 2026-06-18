using eNote.Domain.Entities;

namespace eNote.Application.Features.Assignments;

public static class AssignmentSearchExtensions
{
    public static IQueryable<Assignment> ApplySearch(this IQueryable<Assignment> query, AssignmentSearchObject search)
    {
        if (!string.IsNullOrWhiteSpace(search.Title))
        {
            query = query.Where(x => x.Title.Contains(search.Title));
        }

        if (search.DueAfter.HasValue)
        {
            query = query.Where(x => x.DueAt >= search.DueAfter.Value);
        }

        if (search.DueBefore.HasValue)
        {
            query = query.Where(x => x.DueAt <= search.DueBefore.Value);
        }

        return query;
    }
}
