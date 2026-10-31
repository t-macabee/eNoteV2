using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Payments;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace eNote.Tests.Academic;

public sealed class CourseEnrollmentServiceTests
{
    private static readonly DateTime Now = new(2026, 6, 22, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task EnrollAsync_CreatesPendingRequest_ForPublishedCourse()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        var service = CreateService(context, student);

        await service.EnrollAsync(course.Id);

        var enrollment = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Pending, enrollment.EnrollmentStatus);
        Assert.Equal(student.AppUserId, enrollment.CreatedById);
    }

    [Fact]
    public async Task EnrollAsync_ReopensCanceledEnrollment_AsPending()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        context.Set<Enrollment>().Add(new Enrollment(student.Id, course.Id, EnrollmentStatus.Canceled));
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await service.EnrollAsync(course.Id);

        var enrollment = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Pending, enrollment.EnrollmentStatus);
        Assert.Equal(student.AppUserId, enrollment.UpdatedById);
    }

    [Fact]
    public async Task EnrollAsync_Reenroll_PreservesPaidUntil()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        var enrollment = new Enrollment(student.Id, course.Id, EnrollmentStatus.Active);
        enrollment.ExtendPaidUntil(Now.AddDays(-60), 30);
        context.Set<Enrollment>().Add(enrollment);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await service.UnenrollAsync(course.Id);
        var canceled = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Canceled, canceled.EnrollmentStatus);
        Assert.Equal(Now.AddDays(-30), canceled.PaidUntil);

        await service.EnrollAsync(course.Id);
        var reactivated = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Pending, reactivated.EnrollmentStatus);
        Assert.Equal(Now.AddDays(-30), reactivated.PaidUntil);
    }

    [Fact]
    public async Task EnrollAsync_ResumesCanceledEnrollment_WithPaidTimeLeft()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        var enrollment = new Enrollment(student.Id, course.Id, EnrollmentStatus.Pending);
        enrollment.Transition(EnrollmentTrigger.Approve, userId: 77, now: Now);
        enrollment.ExtendPaidUntil(Now, 14);
        enrollment.Transition(EnrollmentTrigger.Cancel, userId: 77, now: Now.AddDays(1));
        context.Set<Enrollment>().Add(enrollment);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var status = await service.EnrollAsync(course.Id);

        Assert.Equal(EnrollmentStatus.Active, status);
        var stored = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Active, stored.EnrollmentStatus);
        Assert.Equal(Now.AddDays(14), stored.PaidUntil);
        Assert.Equal(77, stored.DecidedById);
        Assert.Equal(1, await context.Set<Enrollment>().CountAsync(x => x.StudentId == student.Id && x.CourseId == course.Id));
    }

    [Fact]
    public async Task EnrollAsync_RequestsAgain_WhenCanceledPaidTimeHasPassed()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        var enrollment = new Enrollment(student.Id, course.Id, EnrollmentStatus.Pending);
        enrollment.Transition(EnrollmentTrigger.Approve, userId: 77, now: Now.AddDays(-31));
        enrollment.ExtendPaidUntil(Now.AddDays(-31), 30);
        enrollment.Transition(EnrollmentTrigger.Cancel, userId: 77, now: Now.AddDays(-30));
        context.Set<Enrollment>().Add(enrollment);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var status = await service.EnrollAsync(course.Id);

        Assert.Equal(EnrollmentStatus.Pending, status);
        var stored = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Pending, stored.EnrollmentStatus);
        Assert.Null(stored.DecidedById);
    }

    [Fact]
    public async Task EnrollAsync_ReturnsStatus()
    {
        await using (var context = CreateContext())
        {
            var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
            var service = CreateService(context, student);

            Assert.Equal(EnrollmentStatus.Pending, await service.EnrollAsync(course.Id));
        }

        await using (var context = CreateContext())
        {
            var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
            context.Set<Enrollment>().Add(new Enrollment(student.Id, course.Id, EnrollmentStatus.Pending));
            await context.SaveChangesAsync();
            var service = CreateService(context, student);

            Assert.Equal(EnrollmentStatus.Pending, await service.EnrollAsync(course.Id));
        }

        await using (var context = CreateContext())
        {
            var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
            context.Set<Enrollment>().Add(new Enrollment(student.Id, course.Id, EnrollmentStatus.Active));
            await context.SaveChangesAsync();
            var service = CreateService(context, student);

            Assert.Equal(EnrollmentStatus.Active, await service.EnrollAsync(course.Id));
        }
    }

    [Fact]
    public async Task EnrollAsync_DoesNotResume_WhenMembershipExpired()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: false);
        var enrollment = new Enrollment(student.Id, course.Id, EnrollmentStatus.Canceled);
        enrollment.ExtendPaidUntil(Now, 14);
        context.Set<Enrollment>().Add(enrollment);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.EnrollAsync(course.Id));

        Assert.Equal(Messages.MembershipInactive, ex.Message);
        var stored = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Canceled, stored.EnrollmentStatus);
    }

    [Fact]
    public async Task EnrollAsync_ReopensRejectedEnrollment_AsPending_AndClearsDecision()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        var rejected = new Enrollment(student.Id, course.Id, EnrollmentStatus.Pending);
        rejected.Transition(EnrollmentTrigger.Reject, userId: 300, now: Now, reason: "Popunjeno");
        context.Set<Enrollment>().Add(rejected);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await service.EnrollAsync(course.Id);

        var enrollment = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Pending, enrollment.EnrollmentStatus);
        Assert.Equal(student.AppUserId, enrollment.UpdatedById);
        Assert.Null(enrollment.DecidedById);
        Assert.Null(enrollment.DecidedAt);
        Assert.Null(enrollment.DecisionNote);
    }

    [Fact]
    public async Task EnrollAsync_IsNoOp_WhenPending()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        context.Set<Enrollment>().Add(new Enrollment(student.Id, course.Id, EnrollmentStatus.Pending));
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await service.EnrollAsync(course.Id);

        var enrollment = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Pending, enrollment.EnrollmentStatus);
        Assert.Null(enrollment.UpdatedById);
    }

    [Fact]
    public async Task EnrollAsync_IsNoOp_WhenAlreadyActive()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        context.Set<Enrollment>().Add(new Enrollment(student.Id, course.Id, EnrollmentStatus.Active));
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await service.EnrollAsync(course.Id);

        var enrollment = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Active, enrollment.EnrollmentStatus);
        Assert.Null(enrollment.UpdatedById);
    }

    [Fact]
    public async Task EnrollAsync_Throws_WhenCourseCompleted()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        context.Set<Enrollment>().Add(new Enrollment(student.Id, course.Id, EnrollmentStatus.Completed));
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await Assert.ThrowsAsync<BusinessException>(() => service.EnrollAsync(course.Id));

        var enrollment = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Completed, enrollment.EnrollmentStatus);
    }

    [Fact]
    public async Task UnenrollAsync_CancelsPendingRequest()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        context.Set<Enrollment>().Add(new Enrollment(student.Id, course.Id, EnrollmentStatus.Pending));
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await service.UnenrollAsync(course.Id);

        var enrollment = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Canceled, enrollment.EnrollmentStatus);
        Assert.Equal(student.AppUserId, enrollment.UpdatedById);
    }

    [Fact]
    public async Task UnenrollAsync_TreatsConcurrencyViolation_AsConflict()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        context.Set<Enrollment>().Add(new Enrollment(student.Id, course.Id, EnrollmentStatus.Active));
        await context.SaveChangesAsync();
        var service = CreateService(new ThrowingSaveDbContext(context, new DbUpdateConcurrencyException("Concurrency conflict.")), student);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.UnenrollAsync(course.Id));

        Assert.Equal(Messages.ConcurrencyConflict, ex.Message);
    }

    [Fact]
    public async Task UnenrollAsync_CancelsActiveEnrollment()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        context.Set<Enrollment>().Add(new Enrollment(student.Id, course.Id, EnrollmentStatus.Active));
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await service.UnenrollAsync(course.Id);

        var enrollment = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Canceled, enrollment.EnrollmentStatus);
        Assert.Equal(student.AppUserId, enrollment.UpdatedById);
    }

    [Fact]
    public async Task UnenrollAsync_OnlyCancelsCurrentStudentsEnrollment()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        var otherStudent = new Student(appUserId: 101, enrollmentDate: Now);
        context.Set<Student>().Add(otherStudent);
        await context.SaveChangesAsync();
        var otherEnrollment = new Enrollment(otherStudent.Id, course.Id, EnrollmentStatus.Active);
        context.Set<Enrollment>().Add(otherEnrollment);
        await context.SaveChangesAsync();
        var enrollment = new Enrollment(student.Id, course.Id, EnrollmentStatus.Active);
        context.Set<Enrollment>().Add(enrollment);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await service.UnenrollAsync(course.Id);

        var storedOther = await context.Set<Enrollment>().AsNoTracking().SingleAsync(e => e.Id == otherEnrollment.Id);
        Assert.Equal(EnrollmentStatus.Active, storedOther.EnrollmentStatus);
        Assert.Null(storedOther.UpdatedById);
        var stored = await context.Set<Enrollment>().AsNoTracking().SingleAsync(e => e.Id == enrollment.Id);
        Assert.Equal(EnrollmentStatus.Canceled, stored.EnrollmentStatus);
    }

    [Fact]
    public async Task UnenrollAsync_DoesNotCancelAnotherEnrollmentsPayment()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        var enrollment = new Enrollment(student.Id, course.Id, EnrollmentStatus.Active);
        context.Set<Enrollment>().Add(enrollment);
        await context.SaveChangesAsync();
        var otherStudent = new Student(appUserId: 101, enrollmentDate: Now);
        context.Set<Student>().Add(otherStudent);
        await context.SaveChangesAsync();
        var otherEnrollment = new Enrollment(otherStudent.Id, course.Id, EnrollmentStatus.Active);
        context.Set<Enrollment>().Add(otherEnrollment);
        await context.SaveChangesAsync();
        context.Set<CoursePayment>().Add(new CoursePayment(otherEnrollment.Id, "pi_other_enrollment", 10000, "bam", PaymentStatus.RequiresAction));
        await context.SaveChangesAsync();
        var gateway = new FakePaymentGateway();
        gateway.AddIntent("pi_other_enrollment", 10000, "bam");
        var service = CreateService(context, student, gateway);

        await service.UnenrollAsync(course.Id);

        Assert.Empty(gateway.CancelCalls);
        var payment = await context.Set<CoursePayment>().AsNoTracking().SingleAsync();
        Assert.Equal(PaymentStatus.RequiresAction, payment.Status);
        var storedOther = await context.Set<Enrollment>().AsNoTracking().SingleAsync(e => e.Id == otherEnrollment.Id);
        Assert.Equal(EnrollmentStatus.Active, storedOther.EnrollmentStatus);
    }

    [Fact]
    public async Task UnenrollAsync_Blocks_WhenFailedAttemptIsProcessing()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        var enrollment = new Enrollment(student.Id, course.Id, EnrollmentStatus.Active);
        context.Set<Enrollment>().Add(enrollment);
        await context.SaveChangesAsync();
        context.Set<CoursePayment>().Add(new CoursePayment(enrollment.Id, "pi_failed_processing", 10000, "bam", PaymentStatus.Failed));
        await context.SaveChangesAsync();
        var gateway = new FakePaymentGateway();
        gateway.AddIntent("pi_failed_processing", 10000, "bam", PaymentIntentStatus.Processing);
        var service = CreateService(context, student, gateway);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.UnenrollAsync(course.Id));

        Assert.Equal(Messages.UnenrollBlockedByPendingPayment, ex.Message);
        Assert.Empty(gateway.CancelCalls);
        var payment = await context.Set<CoursePayment>().AsNoTracking().SingleAsync();
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        var stored = await context.Set<Enrollment>().AsNoTracking().SingleAsync(e => e.Id == enrollment.Id);
        Assert.Equal(EnrollmentStatus.Active, stored.EnrollmentStatus);
    }

    [Fact]
    public async Task UnenrollAsync_CancelsOpenIntentOnStripe_ThenCancelsEnrollment()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        var enrollment = new Enrollment(student.Id, course.Id, EnrollmentStatus.Active);
        context.Set<Enrollment>().Add(enrollment);
        await context.SaveChangesAsync();
        context.Set<CoursePayment>().Add(new CoursePayment(enrollment.Id, "pi_open", 10000, "bam", PaymentStatus.RequiresAction));
        await context.SaveChangesAsync();
        var gateway = new FakePaymentGateway();
        gateway.AddIntent("pi_open", 10000, "bam");
        var service = CreateService(context, student, gateway);

        await service.UnenrollAsync(course.Id);

        Assert.Single(gateway.CancelCalls);
        Assert.Equal("pi_open", gateway.CancelCalls[0].PaymentIntentId);
        Assert.Equal(PaymentIntentStatus.Canceled, (await gateway.RetrievePaymentIntentAsync("pi_open")).Status);
        var payment = await context.Set<CoursePayment>().SingleAsync();
        Assert.Equal(PaymentStatus.Canceled, payment.Status);
        var stored = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Canceled, stored.EnrollmentStatus);
    }

    [Fact]
    public async Task UnenrollAsync_Blocks_WhenIntentIsProcessing()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        var enrollment = new Enrollment(student.Id, course.Id, EnrollmentStatus.Active);
        context.Set<Enrollment>().Add(enrollment);
        await context.SaveChangesAsync();
        context.Set<CoursePayment>().Add(new CoursePayment(enrollment.Id, "pi_processing", 10000, "bam", PaymentStatus.RequiresAction));
        await context.SaveChangesAsync();
        var gateway = new FakePaymentGateway();
        gateway.AddIntent("pi_processing", 10000, "bam", PaymentIntentStatus.Processing);
        var service = CreateService(context, student, gateway);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.UnenrollAsync(course.Id));

        Assert.Equal(Messages.UnenrollBlockedByPendingPayment, ex.Message);
        Assert.Empty(gateway.CancelCalls);
        var stored = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Active, stored.EnrollmentStatus);
    }

    [Theory]
    [InlineData(EnrollmentStatus.Canceled)]
    [InlineData(EnrollmentStatus.Completed)]
    public async Task UnenrollAsync_Throws_WhenEnrollmentIsAlreadyFinished(EnrollmentStatus status)
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        context.Set<Enrollment>().Add(new Enrollment(student.Id, course.Id, status));
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.UnenrollAsync(course.Id));

        Assert.Equal(Messages.StudentNotEnrolled, ex.Message);
        var stored = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(status, stored.EnrollmentStatus);
    }

    [Fact]
    public async Task UnenrollAsync_Succeeds_WhenMembershipIsInactive()
    {

        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: false);
        context.Set<Enrollment>().Add(new Enrollment(student.Id, course.Id, EnrollmentStatus.Active));
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await service.UnenrollAsync(course.Id);

        var enrollment = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Canceled, enrollment.EnrollmentStatus);
    }

    [Fact]
    public async Task EnrollAsync_Throws_WhenMembershipIsInactive()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: false);
        var service = CreateService(context, student);

        await Assert.ThrowsAsync<BusinessException>(() => service.EnrollAsync(course.Id));
    }

    [Fact]
    public async Task EnrollAsync_Throws_WhenCourseEnded()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        course.UpdateDetails(course.Name, course.Description, course.Price, course.StartDate, Now.AddDays(-1));
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.EnrollAsync(course.Id));

        Assert.Equal(Messages.CourseEnded, ex.Message);
        Assert.Empty(context.Set<Enrollment>());
    }

    [Fact]
    public async Task EnrollAsync_AllowsRequest_OnCourseEndDay()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        course.UpdateDetails(course.Name, course.Description, course.Price, course.StartDate, Now.Date);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await service.EnrollAsync(course.Id);

        var enrollment = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Pending, enrollment.EnrollmentStatus);
    }

    [Fact]
    public async Task EnrollAsync_Throws_WhenCourseUnpublished()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        course.SetPublishedStatus(false);
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await Assert.ThrowsAsync<NotFoundException>(() => service.EnrollAsync(course.Id));
        Assert.Empty(context.Set<Enrollment>());
    }

    private static ENoteContext CreateContext() =>
        new(TestDbContextFactory.CreateOptions(), new FixedClock(Now), new StubCurrentActor(storeId: 1, userId: 100)) { ExplicitStoreId = 1 };

    private static async Task<(Student Student, Course Course)> SeedStudentAndCourseAsync(ENoteContext context, bool hasActiveMembership)
    {
        var student = new Student(appUserId: 100, enrollmentDate: Now.AddMonths(-1));
        student.UpdateMembership(hasActiveMembership ? Now.AddDays(1) : Now.AddDays(-1));

        var instructor = new Instructor(appUserId: 200);
        context.Set<Student>().Add(student);
        context.Set<Instructor>().Add(instructor);
        await context.SaveChangesAsync();

        var course = new Course("Theory", null, 10, null, null, instructor.Id);
        course.SetPublishedStatus(true);
        context.Set<Course>().Add(course);
        await context.SaveChangesAsync();

        return (student, course);
    }

    [Fact]
    public async Task EnrollAsync_TreatsDuplicateEnrollmentViolation_AsConflict()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        var inner = new Exception($"duplicate key value violates unique constraint \"{DbConstraintNames.EnrollmentStudentIdCourseIdUniqueIndex}\"");
        var service = CreateService(new ThrowingSaveDbContext(context, new DbUpdateException("Unique constraint violated.", inner)), student);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.EnrollAsync(course.Id));

        Assert.Equal(Messages.AlreadyEnrolled, ex.Message);
    }

    [Fact]
    public async Task EnrollAsync_TreatsConcurrencyFailure_AsConflict()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        var service = CreateService(new ThrowingSaveDbContext(context, new DbUpdateConcurrencyException()), student);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.EnrollAsync(course.Id));

        Assert.Equal(Messages.ConcurrencyConflict, ex.Message);
    }

    [Fact]
    public async Task EnrollAsync_DoesNotHideOtherSaveFailures_AsConflict()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        var service = CreateService(new ThrowingSaveDbContext(context, new DbUpdateException("Other failure.", new Exception("connection lost"))), student);

        await Assert.ThrowsAsync<DbUpdateException>(() => service.EnrollAsync(course.Id));
    }

    [Fact]
    public async Task EnrollAsync_Throws_WhenCourseDoesNotExist_EvenIfOtherCoursesArePublished()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        var service = CreateService(context, student);

        await Assert.ThrowsAsync<NotFoundException>(() => service.EnrollAsync(course.Id + 1000));
        Assert.Empty(context.Set<Enrollment>());
    }

    [Fact]
    public async Task EnrollAsync_IgnoresEnrollmentOfAnotherStudent()
    {
        await using var context = CreateContext();
        var (student, course) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        var other = new Student(appUserId: 101, enrollmentDate: Now.AddMonths(-1));
        context.Set<Student>().Add(other);
        await context.SaveChangesAsync();
        context.Set<Enrollment>().Add(new Enrollment(other.Id, course.Id, EnrollmentStatus.Active));
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await service.EnrollAsync(course.Id);

        var own = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == student.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Pending, own.EnrollmentStatus);
        var others = await context.Set<Enrollment>().SingleAsync(x => x.StudentId == other.Id && x.CourseId == course.Id);
        Assert.Equal(EnrollmentStatus.Active, others.EnrollmentStatus);
    }

    [Fact]
    public async Task EnrollAsync_AllowsSecondCourse_WhileActiveInFirst()
    {
        await using var context = CreateContext();
        var (student, first) = await SeedStudentAndCourseAsync(context, hasActiveMembership: true);
        var second = new Course("Practice", null, 10, null, null, first.InstructorId);
        second.SetPublishedStatus(true);
        context.Set<Course>().Add(second);
        context.Set<Enrollment>().Add(new Enrollment(student.Id, first.Id, EnrollmentStatus.Active));
        await context.SaveChangesAsync();
        var service = CreateService(context, student);

        await service.EnrollAsync(second.Id);

        var enrollments = await context.Set<Enrollment>().Where(x => x.StudentId == student.Id).ToListAsync();
        Assert.Equal(2, enrollments.Count);
        Assert.Equal(EnrollmentStatus.Active, enrollments.Single(x => x.CourseId == first.Id).EnrollmentStatus);
        Assert.Equal(EnrollmentStatus.Pending, enrollments.Single(x => x.CourseId == second.Id).EnrollmentStatus);
    }

    private static CourseEnrollmentService CreateService(IAppDbContext context, Student student, IPaymentGateway? gateway = null)
    {
        var currentUser = new StubCurrentActor(student: student);
        return new(
            context,
            new FixedClock(Now),
            currentUser,
            currentUser,
            gateway ?? new FakePaymentGateway(),
            NullLogger<CourseEnrollmentService>.Instance);
    }
}
