using eNote.Application.Features.Identity.Instructors;

namespace eNote.Tests.TestUtils;

public static class AcademicTestData
{
    public static async Task<AcademicHarness> SeedAsync(ENoteContext context, DateTime now)
    {
        var instructor = new Instructor(100);
        context.Set<Instructor>().Add(instructor);
        await context.SaveChangesAsync();

        var course = new Course("Guitar 101", null, 100m, now, now.AddMonths(6), instructor.Id)
        {
            CreatedById = instructor.AppUserId
        };
        course.SetPublishedStatus(true);
        context.Set<Course>().Add(course);
        await context.SaveChangesAsync();

        var lecture = new Lecture("First lesson", "Room 1", 60, now, LectureType.Theoretical, 30, course.Id)
        {
            CreatedById = instructor.AppUserId
        };
        context.Set<Lecture>().Add(lecture);
        await context.SaveChangesAsync();

        var student = new Student(50, now);
        context.Set<Student>().Add(student);
        await context.SaveChangesAsync();

        var enrollment = new Enrollment(student.Id, course.Id, EnrollmentStatus.Active);
        enrollment.ExtendPaidUntil(now, 30);
        context.Set<Enrollment>().Add(enrollment);
        await context.SaveChangesAsync();

        return new AcademicHarness(context, instructor, course, lecture, student);
    }

    public static InstructorAccessService CreateInstructorAccess(ENoteContext context, Instructor instructor) =>
        new(context, new StubUserProfileLookup(instructor: instructor));

    public static T CreateService<T>(
        ENoteContext context, Instructor instructor, StubCurrentActor? actor = null, DateTime? now = null)
        where T : class
    {
        var currentUser = actor ?? new StubCurrentActor(instructor: instructor);
        var effectiveNow = now
            ?? context.Set<Course>().Local.FirstOrDefault()?.StartDate
            ?? context.Set<Course>().Select(c => (DateTime?)c.StartDate).FirstOrDefault()
            ?? new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        return (T)Activator.CreateInstance(
            typeof(T),
            context,
            currentUser,
            currentUser,
            CreateInstructorAccess(context, instructor),
            TestMapper.Create(),
            new FixedClock(effectiveNow))!;
    }
}

public sealed record AcademicHarness(
    ENoteContext Context,
    Instructor Instructor,
    Course Course,
    Lecture Lecture,
    Student Student);
