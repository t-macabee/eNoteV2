using eNote.Application.Common.Search;

namespace eNote.Application.Features.Academic.Assignments;

public static class AssignmentSearchExtensions
{
    public static IQueryable<Assignment> ApplySearch(this IQueryable<Assignment> query, AssignmentSearchObject search) =>
        query
            .WhereContainsIf(search.Title, x => x.Title.Contains(search.Title!))
            .WhereEqualsIf(search.DueAfter, x => x.DueAt >= search.DueAfter!.Value)
            .WhereEqualsIf(search.DueBefore, x => x.DueAt <= search.DueBefore!.Value);

    public static IQueryable<Assignment> ForEnrolledStudentById(this IQueryable<Assignment> query, int studentId, int assignmentId) =>
        query.ForEnrolledStudent(studentId).Where(x => x.Id == assignmentId);
}