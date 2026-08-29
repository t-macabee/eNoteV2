namespace eNote.Application.Features.Reports.Services;

public interface IReportService
{
    Task<byte[]> GenerateCourseRankingPdfAsync(int courseId, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateStoreRentalSummaryPdfAsync(CancellationToken cancellationToken = default);
    Task<byte[]> GenerateLectureAttendancePdfAsync(int lectureId, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateAdminMusicStoreReportAsync(CancellationToken cancellationToken = default);
}