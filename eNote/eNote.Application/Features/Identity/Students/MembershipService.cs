using eNote.Application.Constants;
using eNote.Application.Features.Identity.Students.Services;

namespace eNote.Application.Features.Identity.Students;

public sealed class MembershipService(
    IAppDbContext context,
    AdminStudentService students,
    IClock clock,
    IMembershipWarningDispatcher notifications)
{
    public const int WarningWindowDays = 7;
    public const int WarningRepeatHours = 24;

    public async Task<MembershipPaymentDto> RecordPaymentAsync(
        int? instructorId,
        int studentId,
        RecordMembershipPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        Student student = await LoadStudentAsync(instructorId, studentId, cancellationToken);

        var today = clock.UtcNow.Date;
        var periodStart = student.MembershipPaidUntil.HasValue && student.MembershipPaidUntil.Value.Date > today
            ? student.MembershipPaidUntil.Value.Date
            : today;
        var periodEnd = request.PeriodEnd.HasValue
            ? request.PeriodEnd.Value.Date
            : periodStart.AddMonths(request.Months!.Value);

        if (periodEnd <= periodStart)
        {
            throw new BusinessException(Messages.MembershipPeriodEndBeforeStart);
        }

        var payment = new MembershipPayment(
            student.Id,
            request.Amount,
            request.PaidAt.HasValue ? request.PaidAt.Value.Date : today,
            periodStart,
            periodEnd,
            request.ReceiptNumber,
            request.Note);

        context.Set<MembershipPayment>().Add(payment);
        student.UpdateMembership(periodEnd);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (DbErrors.IsUniqueViolation(exception, DbConstraintNames.MembershipPaymentReceiptNumberUniqueIndex))
        {
            throw new BusinessException(Messages.MembershipReceiptTaken);
        }

        return Map(payment);
    }

    public async Task<PagedResult<MembershipPaymentDto>> GetPaymentsAsync(
        int? instructorId,
        int studentId,
        MembershipPaymentSearchObject search,
        CancellationToken cancellationToken = default)
    {
        await EnsureVisibleAsync(instructorId, studentId, cancellationToken);

        return await context.Set<MembershipPayment>()
            .AsNoTracking()
            .Where(p => p.StudentId == studentId)
            .ToPagedResultAsync(
                search,
                Map,
                query => query.OrderByDescending(p => p.PaidAt),
                cancellationToken);
    }

    public async Task SendWarningAsync(
        int? instructorId,
        int studentId,
        SendMembershipWarningRequest request,
        CancellationToken cancellationToken = default)
    {
        Student student = await LoadStudentAsync(instructorId, studentId, cancellationToken);
        var now = clock.UtcNow;

        if (!IsWarningDue(student.MembershipPaidUntil, now))
        {
            throw new BusinessException(Messages.MembershipWarningNotAllowed);
        }

        if (student.MembershipWarningSentAt.HasValue && student.MembershipWarningSentAt.Value > now.AddHours(-WarningRepeatHours))
        {
            throw new BusinessException(Messages.MembershipWarningRecentlySent);
        }

        student.RecordMembershipWarning(now);
        await notifications.DispatchWarningAsync(student.AppUserId, student.MembershipPaidUntil, request.Message);
        await context.SaveChangesAsync(cancellationToken);
    }

    public static bool IsWarningDue(DateTime? membershipPaidUntil, DateTime utcNow) =>
        !membershipPaidUntil.HasValue || membershipPaidUntil.Value.Date <= utcNow.Date.AddDays(WarningWindowDays);

    private async Task<Student> LoadStudentAsync(int? instructorId, int studentId, CancellationToken cancellationToken)
    {
        await EnsureVisibleAsync(instructorId, studentId, cancellationToken);

        return await context.Set<Student>().FirstAsync(s => s.Id == studentId, cancellationToken);
    }

    private async Task EnsureVisibleAsync(int? instructorId, int studentId, CancellationToken cancellationToken)
    {
        var visible = instructorId.HasValue
            ? await students.StudentsVisibleTo(instructorId.Value).AnyAsync(s => s.Id == studentId, cancellationToken)
            : await context.Set<Student>().AsNoTracking().AnyAsync(s => s.Id == studentId, cancellationToken);

        if (!visible)
        {
            throw new NotFoundException(Messages.StudentProfileNotFound);
        }
    }

    private static MembershipPaymentDto Map(MembershipPayment payment) => new(
        payment.Id,
        payment.StudentId,
        payment.Amount,
        payment.PaidAt,
        payment.PeriodStart,
        payment.PeriodEnd,
        payment.ReceiptNumber,
        payment.Note);
}
