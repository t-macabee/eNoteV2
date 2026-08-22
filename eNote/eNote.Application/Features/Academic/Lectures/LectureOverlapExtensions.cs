namespace eNote.Application.Features.Academic.Lectures;

public static class LectureOverlapExtensions
{
    public static Task<bool> HasLocationConflictAsync(this IQueryable<Lecture> query, string location, DateTime lectureTime, int duration, int? excludedLectureId = null, CancellationToken cancellationToken = default) =>
        query.AnyAsync(x =>
            x.Id != excludedLectureId &&
            x.LectureStatus != LectureStatus.Cancelled &&
            x.Location.ToLower() == location &&
            x.LectureTime < lectureTime.AddMinutes(duration) &&
            x.LectureTime.AddMinutes(x.Duration) > lectureTime,
            cancellationToken);

    public static Task<bool> HasInstructorConflictAsync(this IQueryable<Lecture> query, int instructorId, DateTime lectureTime, int duration, int? excludedLectureId = null, CancellationToken cancellationToken = default) =>
        query.AnyAsync(x =>
            x.Id != excludedLectureId &&
            x.LectureStatus != LectureStatus.Cancelled &&
            x.Course.InstructorId == instructorId &&
            x.LectureTime < lectureTime.AddMinutes(duration) &&
            x.LectureTime.AddMinutes(x.Duration) > lectureTime,
            cancellationToken);
}
