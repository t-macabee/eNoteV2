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

        context.Set<Enrollment>().Add(new Enrollment(student.Id, course.Id, EnrollmentStatus.Active));
        await context.SaveChangesAsync();

        return new AcademicHarness(context, instructor, course, lecture, student);
    }

    public static InstructorAccessService CreateInstructorAccess(ENoteContext context, Instructor instructor) =>
        new(context, new StubUserProfileLookup(instructor: instructor));
}

public sealed record AcademicHarness(
    ENoteContext Context,
    Instructor Instructor,
    Course Course,
    Lecture Lecture,
    Student Student);
