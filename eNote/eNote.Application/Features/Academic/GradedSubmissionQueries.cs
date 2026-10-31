namespace eNote.Application.Features.Academic;

internal static class GradedSubmissionQueries
{
    public static IQueryable<AssignmentSubmission> CountedGrades(IAppDbContext context) =>
        context.Set<AssignmentSubmission>()
            .AsNoTracking()
            .Where(s => s.Grade != null
                && (s.Assignment.Lecture.StudentId == null || s.Assignment.Lecture.StudentId == s.StudentId));

    public static double? RoundAverage(double? average) =>
        average.HasValue ? Math.Round(average.Value, 2) : null;
}
