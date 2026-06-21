using eNote.Application.Common.Search;
using eNote.Domain.Entities;

namespace eNote.Application.Features.Assignments;

public static class AssignmentSearchExtensions
{
    public static IQueryable<Assignment> ApplySearch(this IQueryable<Assignment> query, AssignmentSearchObject search) =>
        query
            .WhereContainsIf(search.Title, x => x.Title.Contains(search.Title!))
            .WhereEqualsIf(search.DueAfter, x => x.DueAt >= search.DueAfter!.Value)
            .WhereEqualsIf(search.DueBefore, x => x.DueAt <= search.DueBefore!.Value);
}