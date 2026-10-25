using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.TestUtils;

public static class TuitionTestData
{
    public static async Task<(ENoteContext Context, Student Student, Course Course, Enrollment Enrollment)> SetupScenarioAsync(DateTime now, decimal price, string? databaseName = null)
    {
        var options = TestDbContextFactory.CreateOptions(databaseName);

        var student = new Student(50, now);
        var instructor = new Instructor(100);
        var context = new ENoteContext(options, new FixedClock(now), new StubCurrentActor(student: student)) { ExplicitStoreId = 1 };
        context.Set<Student>().Add(student);
        context.Set<Instructor>().Add(instructor);
        await context.SaveChangesAsync();

        var course = new Course("Guitar", null, price, now, now.AddMonths(6), instructor.Id);
        course.SetPublishedStatus(true);
        context.Set<Course>().Add(course);
        await context.SaveChangesAsync();

        var enrollment = new Enrollment(student.Id, course.Id, EnrollmentStatus.Active);
        context.Set<Enrollment>().Add(enrollment);
        await context.SaveChangesAsync();

        return (context, student, course, enrollment);
    }
}
