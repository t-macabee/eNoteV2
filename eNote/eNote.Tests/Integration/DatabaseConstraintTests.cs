using System.Reflection;
using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Domain.Entities.Assignments;
using eNote.Infrastructure.Identity;
using eNote.Tests.TestUtils;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class DatabaseConstraintTests(SqlServerFixture fixture)
{
    private static readonly DateTime Now = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Model_HasNoChangesWithoutMigration()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        var pending = await context.Database.GetPendingMigrationsAsync();

        Assert.Empty(pending);
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task ConstraintNames_ExistInDatabase()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        var databaseNames = (await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT name AS Value FROM sys.indexes WHERE name IS NOT NULL
                UNION
                SELECT name AS Value FROM sys.check_constraints
                """)
            .ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var constraintNames = typeof(DbConstraintNames)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToList();

        Assert.NotEmpty(constraintNames);
        Assert.All(constraintNames, name => Assert.Contains(name, databaseNames));
    }

    [Fact]
    public async Task HistoryForeignKeys_BlockDeletesExceptProfileLinks()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        var foreignKeys = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT CONCAT(
                    fk.name COLLATE DATABASE_DEFAULT,
                    N'|' COLLATE DATABASE_DEFAULT,
                    OBJECT_NAME(fk.parent_object_id) COLLATE DATABASE_DEFAULT,
                    N'|' COLLATE DATABASE_DEFAULT,
                    OBJECT_NAME(fk.referenced_object_id) COLLATE DATABASE_DEFAULT,
                    N'|' COLLATE DATABASE_DEFAULT,
                    fk.delete_referential_action_desc COLLATE DATABASE_DEFAULT) AS Value
                FROM sys.foreign_keys AS fk
                WHERE OBJECT_NAME(fk.referenced_object_id) IN (N'AspNetUsers', N'Student', N'Instructor')
                  AND OBJECT_NAME(fk.parent_object_id) NOT LIKE N'AspNetUser%'
                """)
            .ToListAsync();

        var allowedCascades = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "FK_Student_AspNetUsers_AppUserId",
            "FK_Instructor_AspNetUsers_AppUserId",
            "FK_Notification_AspNetUsers_UserId",
            "FK_InstrumentView_AspNetUsers_UserId"
        };

        Assert.NotEmpty(foreignKeys);

        foreach (var foreignKey in foreignKeys)
        {
            var parts = foreignKey.Split('|');
            Assert.Equal(4, parts.Length);

            var (name, parentTable, referencedTable, deleteAction) = (parts[0], parts[1], parts[2], parts[3]);
            var expectedAction = allowedCascades.Contains(name) ? "CASCADE" : "NO_ACTION";

            Assert.True(
                string.Equals(expectedAction, deleteAction, StringComparison.Ordinal),
                $"{name} ({parentTable} -> {referencedTable}) has delete action {deleteAction}, expected {expectedAction}.");
        }
    }

    [Fact]
    public async Task DuplicateStripeWebhookEventId_IsUniqueViolation()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        context.Set<StripeWebhookEvent>().Add(new StripeWebhookEvent("evt_duplicate", "payment_intent.succeeded", "{}", Now));
        await context.SaveChangesAsync();

        context.Set<StripeWebhookEvent>().Add(new StripeWebhookEvent("evt_duplicate", "payment_intent.succeeded", "{}", Now));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.True(DbErrors.IsUniqueViolation(exception, DbConstraintNames.StripeWebhookEventStripeEventIdUniqueIndex));
    }

    [Theory]
    [InlineData(PaymentStatus.Succeeded)]
    [InlineData(PaymentStatus.Canceled)]
    [InlineData(PaymentStatus.Refunded)]
    public async Task CoursePayment_OpenAttemptIndex_AllowsNewAttemptAfterClosed(PaymentStatus closedStatus)
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        var (_, _, _, enrollment) = await SeedCourseEnrollmentAsync(context);

        context.Set<CoursePayment>().Add(
            new CoursePayment(enrollment.Id, "pi_closed_attempt", 10000, "bam", closedStatus));
        await context.SaveChangesAsync();

        context.Set<CoursePayment>().Add(
            new CoursePayment(enrollment.Id, "pi_open_attempt_1", 10000, "bam", PaymentStatus.RequiresAction));
        await context.SaveChangesAsync();

        context.Set<CoursePayment>().Add(
            new CoursePayment(enrollment.Id, "pi_open_attempt_2", 10000, "bam", PaymentStatus.RequiresAction));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.True(DbErrors.IsUniqueViolation(exception, DbConstraintNames.CoursePaymentOpenAttemptUniqueIndex));
    }

    [Theory]
    [InlineData(PaymentStatus.Succeeded)]
    [InlineData(PaymentStatus.Canceled)]
    [InlineData(PaymentStatus.Refunded)]
    public async Task RentalPayment_OpenAttemptIndex_AllowsNewAttemptAfterClosed(PaymentStatus closedStatus)
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        var (student, instrument) = await SeedRentalStudentAndInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();

        context.Set<RentalPayment>().Add(
            new RentalPayment(rental.Id, rental.MusicStoreId, "pi_rental_closed", 10000, "bam", closedStatus));
        await context.SaveChangesAsync();

        context.Set<RentalPayment>().Add(
            new RentalPayment(rental.Id, rental.MusicStoreId, "pi_rental_open_1", 10000, "bam", PaymentStatus.RequiresAction));
        await context.SaveChangesAsync();

        context.Set<RentalPayment>().Add(
            new RentalPayment(rental.Id, rental.MusicStoreId, "pi_rental_open_2", 10000, "bam", PaymentStatus.RequiresAction));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.True(DbErrors.IsUniqueViolation(exception, DbConstraintNames.RentalPaymentOpenAttemptUniqueIndex));
    }

    [Fact]
    public async Task CoursePayment_OpenAttemptIndex_TreatsFailedAsOpen()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        var (_, _, _, enrollment) = await SeedCourseEnrollmentAsync(context);

        context.Set<CoursePayment>().Add(
            new CoursePayment(enrollment.Id, "pi_failed_attempt", 10000, "bam", PaymentStatus.Failed));
        await context.SaveChangesAsync();

        context.Set<CoursePayment>().Add(
            new CoursePayment(enrollment.Id, "pi_requires_action", 10000, "bam", PaymentStatus.RequiresAction));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.True(DbErrors.IsUniqueViolation(exception, DbConstraintNames.CoursePaymentOpenAttemptUniqueIndex));
    }

    [Fact]
    public async Task RentalPayment_OpenAttemptIndex_TreatsFailedAsOpen()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        var (student, instrument) = await SeedRentalStudentAndInstrumentAsync(context);
        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();

        context.Set<RentalPayment>().Add(
            new RentalPayment(rental.Id, rental.MusicStoreId, "pi_rental_failed", 10000, "bam", PaymentStatus.Failed));
        await context.SaveChangesAsync();

        context.Set<RentalPayment>().Add(
            new RentalPayment(rental.Id, rental.MusicStoreId, "pi_rental_requires_action", 10000, "bam", PaymentStatus.RequiresAction));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.True(DbErrors.IsUniqueViolation(exception, DbConstraintNames.RentalPaymentOpenAttemptUniqueIndex));
    }

    [Theory]
    [InlineData(InstrumentRentalStatus.Approved)]
    [InlineData(InstrumentRentalStatus.Active)]
    public async Task InstrumentRental_ActiveOrApprovedIndex_AllowsRentalAfterCompleted(InstrumentRentalStatus status)
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        var (student, instrument) = await SeedRentalStudentAndInstrumentAsync(context);

        var completed = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now.AddDays(-10), null);
        RentalTestData.AdvanceTo(
            completed,
            InstrumentRentalStatus.Completed,
            Now,
            userId: student.AppUserId,
            approvedAt: Now.AddDays(-9),
            pickedUpAt: Now.AddDays(-9),
            finishedAt: Now.AddDays(-3));
        context.Set<InstrumentRental>().Add(completed);
        await context.SaveChangesAsync();

        var rental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        RentalTestData.AdvanceTo(rental, status, Now, userId: student.AppUserId, approvedAt: Now, pickedUpAt: Now);
        context.Set<InstrumentRental>().Add(rental);
        await context.SaveChangesAsync();

        var secondRental = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        RentalTestData.AdvanceTo(secondRental, status, Now, userId: student.AppUserId, approvedAt: Now, pickedUpAt: Now);
        context.Set<InstrumentRental>().Add(secondRental);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.True(DbErrors.IsUniqueViolation(exception, DbConstraintNames.InstrumentRentalActiveOrApprovedUniqueIndex));
    }

    [Fact]
    public async Task InstrumentRental_PendingIndex_AllowsNewRequestAfterCompleted()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        var (student, instrument) = await SeedRentalStudentAndInstrumentAsync(context);

        var completed = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now.AddDays(-10), null);
        RentalTestData.AdvanceTo(
            completed,
            InstrumentRentalStatus.Completed,
            Now,
            userId: student.AppUserId,
            approvedAt: Now.AddDays(-9),
            pickedUpAt: Now.AddDays(-9),
            finishedAt: Now.AddDays(-3));
        context.Set<InstrumentRental>().Add(completed);
        await context.SaveChangesAsync();

        var pending = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        context.Set<InstrumentRental>().Add(pending);
        await context.SaveChangesAsync();

        var secondPending = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        context.Set<InstrumentRental>().Add(secondPending);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.True(DbErrors.IsUniqueViolation(exception, DbConstraintNames.InstrumentRentalPendingUniqueIndex));
    }

    [Fact]
    public async Task InstrumentRental_PendingIndex_AllowsPendingRequestFromAnotherStudent()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        var (student, instrument) = await SeedRentalStudentAndInstrumentAsync(context);

        var pending = new InstrumentRental(instrument.Id, student.Id, instrument.MusicStoreId, Now, null);
        context.Set<InstrumentRental>().Add(pending);
        await context.SaveChangesAsync();

        var otherUser = new AppUser
        {
            UserName = "other-renter",
            NormalizedUserName = "OTHER-RENTER",
            Email = "other-renter@example.com",
            NormalizedEmail = "OTHER-RENTER@EXAMPLE.COM",
            IsActive = true
        };
        context.Set<AppUser>().Add(otherUser);
        await context.SaveChangesAsync();

        var otherStudent = new Student(otherUser.Id, Now.AddMonths(-1));
        context.Set<Student>().Add(otherStudent);
        await context.SaveChangesAsync();

        var otherPending = new InstrumentRental(instrument.Id, otherStudent.Id, instrument.MusicStoreId, Now, null);
        context.Set<InstrumentRental>().Add(otherPending);
        await context.SaveChangesAsync();

        Assert.Equal(2, await context.Set<InstrumentRental>()
            .CountAsync(r => r.InstrumentId == instrument.Id && r.RentalStatus == InstrumentRentalStatus.Pending));
    }

    [Fact]
    public async Task DuplicateMembershipReceiptNumber_IsUniqueViolation()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        var (student, _, _, _) = await SeedCourseEnrollmentAsync(context);

        context.Set<MembershipPayment>().Add(
            new MembershipPayment(student.Id, 50m, Now, Now, Now.AddMonths(1), "REC-1", null));
        await context.SaveChangesAsync();

        context.Set<MembershipPayment>().Add(
            new MembershipPayment(student.Id, 50m, Now.AddDays(1), Now, Now.AddMonths(1), "REC-1", null));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.True(DbErrors.IsUniqueViolation(exception, DbConstraintNames.MembershipPaymentReceiptNumberUniqueIndex));
    }

    [Fact]
    public async Task DuplicateAssignmentSubmission_IsUniqueViolation()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        var (student, _, _, enrollment) = await SeedCourseEnrollmentAsync(context);

        var lecture = new Lecture("Guitar lesson", "Room 1", 60, Now.AddDays(1), LectureType.Theoretical, 30, enrollment.CourseId);
        context.Set<Lecture>().Add(lecture);
        await context.SaveChangesAsync();

        var assignment = new Assignment("Homework", "Do it", Now.AddDays(7), lecture.Id);
        context.Set<Assignment>().Add(assignment);
        await context.SaveChangesAsync();

        context.Set<AssignmentSubmission>().Add(new AssignmentSubmission(assignment.Id, student.Id));
        await context.SaveChangesAsync();

        context.Set<AssignmentSubmission>().Add(new AssignmentSubmission(assignment.Id, student.Id));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.True(DbErrors.IsUniqueViolation(exception, DbConstraintNames.AssignmentSubmissionAssignmentIdStudentIdUniqueIndex));
    }

    [Fact]
    public async Task DuplicateAttendance_IsUniqueViolation()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        var (student, _, _, enrollment) = await SeedCourseEnrollmentAsync(context);

        var lecture = new Lecture("Guitar lesson", "Room 1", 60, Now.AddDays(1), LectureType.Theoretical, 30, enrollment.CourseId);
        context.Set<Lecture>().Add(lecture);
        await context.SaveChangesAsync();

        context.Set<Attendance>().Add(new Attendance(student.Id, lecture.Id, AttendanceStatus.Present));
        await context.SaveChangesAsync();

        context.Set<Attendance>().Add(new Attendance(student.Id, lecture.Id, AttendanceStatus.Absent));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        Assert.True(DbErrors.IsUniqueViolation(exception, DbConstraintNames.AttendanceStudentIdLectureIdUniqueIndex));
    }

    [Fact]
    public async Task ConcurrentEnrollmentUpdate_ThrowsConcurrencyException()
    {
        var database = await fixture.CreateDatabaseAsync();

        int enrollmentId;

        await using (var seedContext = database.CreateContext())
        {
            var (_, _, _, enrollment) = await SeedCourseEnrollmentAsync(seedContext);
            enrollmentId = enrollment.Id;
        }

        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();

        var first = await firstContext.Set<Enrollment>().SingleAsync(e => e.Id == enrollmentId);
        var second = await secondContext.Set<Enrollment>().SingleAsync(e => e.Id == enrollmentId);

        Assert.True(first.Transition(EnrollmentTrigger.Cancel, 20, Now).IsSuccess);
        Assert.True(second.Transition(EnrollmentTrigger.Complete, 20, Now).IsSuccess);

        await firstContext.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());
    }

    [Fact]
    public async Task CourseWithUnknownCreatedById_IsForeignKeyViolation()
    {
        var database = await fixture.CreateDatabaseAsync();
        await using var context = database.CreateContext();

        var (_, instructor, _, _) = await SeedCourseEnrollmentAsync(context);

        var course = new Course("Guitar", null, 100m, Now, Now.AddMonths(6), instructor.Id) { CreatedById = 999999 };
        context.Set<Course>().Add(course);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        var sqlException = Assert.IsType<SqlException>(exception.InnerException);
        Assert.Equal(547, sqlException.Number);
    }

    private static async Task<(Student Student, Instrument Instrument)> SeedRentalStudentAndInstrumentAsync(ENoteContext context)
    {
        var user = new AppUser
        {
            UserName = "renter",
            NormalizedUserName = "RENTER",
            Email = "renter@example.com",
            NormalizedEmail = "RENTER@EXAMPLE.COM",
            IsActive = true
        };
        context.Set<AppUser>().Add(user);
        await context.SaveChangesAsync();

        var student = new Student(user.Id, Now.AddMonths(-1));
        context.Set<Student>().Add(student);

        var store = new MusicStore("Music Store", "09:00-17:00");
        context.Set<MusicStore>().Add(store);
        await context.SaveChangesAsync();

        var type = new InstrumentType { Type = "Guitar", MonthlyFee = 50m };
        context.Set<InstrumentType>().Add(type);
        await context.SaveChangesAsync();

        var instrument = new Instrument("Model", "Manufacturer", null, null, type.Id, store.Id);
        context.Set<Instrument>().Add(instrument);
        await context.SaveChangesAsync();

        return (student, instrument);
    }

    private static async Task<(Student Student, Instructor Instructor, Course Course, Enrollment Enrollment)> SeedCourseEnrollmentAsync(
        ENoteContext context)
    {
        var studentUser = new AppUser
        {
            UserName = "student",
            NormalizedUserName = "STUDENT",
            Email = "student@example.com",
            NormalizedEmail = "STUDENT@EXAMPLE.COM",
            IsActive = true
        };
        var instructorUser = new AppUser
        {
            UserName = "instructor",
            NormalizedUserName = "INSTRUCTOR",
            Email = "instructor@example.com",
            NormalizedEmail = "INSTRUCTOR@EXAMPLE.COM",
            IsActive = true
        };
        context.Set<AppUser>().AddRange(studentUser, instructorUser);
        await context.SaveChangesAsync();

        var student = new Student(studentUser.Id, Now.AddMonths(-1));
        var instructor = new Instructor(instructorUser.Id);
        context.Set<Student>().Add(student);
        context.Set<Instructor>().Add(instructor);
        await context.SaveChangesAsync();

        var course = new Course("Guitar", null, 100m, Now, Now.AddMonths(6), instructor.Id);
        course.SetPublishedStatus(true);
        context.Set<Course>().Add(course);
        await context.SaveChangesAsync();

        var enrollment = new Enrollment(student.Id, course.Id, EnrollmentStatus.Active);
        context.Set<Enrollment>().Add(enrollment);
        await context.SaveChangesAsync();

        return (student, instructor, course, enrollment);
    }
}
