namespace eNote.Application.Features.Academic.Tuition;

public interface ITuitionPaymentService
{
    Task<CreateTuitionIntentResponse> CreateIntentAsync(int enrollmentId, CancellationToken cancellationToken = default);
    Task<CoursePaymentDto> GetLatestAsync(int enrollmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CoursePaymentDto>> GetHistoryAsync(int enrollmentId, CancellationToken cancellationToken = default);
}
