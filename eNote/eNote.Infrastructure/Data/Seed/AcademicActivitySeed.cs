using eNote.Application.Common.Time;
using eNote.Domain.Entities.Academic;
using eNote.Domain.Entities.Assignments;
using eNote.Domain.Entities.Identity;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eNote.Infrastructure.Data.Seed;

internal static class AcademicActivitySeed
{
    public static async Task SeedAttendance(ENoteContext context, IClock clock)
    {
        if (await context.Set<Attendance>().AnyAsync())
        {
            return;
        }

        var students = await SeedStudents.GetByUsernameAsync(context);
        if (students.Count < SeedStudents.Usernames.Length)
        {
            return;
        }

        List<Student> orderedStudents = [.. SeedStudents.Usernames.Select(u => students[u])];

        var now = clock.UtcNow;
        var pastLectures = await context.Set<Lecture>()
            .Where(l => l.LectureTime < now)
            .OrderBy(l => l.LectureTime)
            .ToListAsync();

        for (int lectureIndex = 0; lectureIndex < pastLectures.Count; lectureIndex++)
        {
            var lecture = pastLectures[lectureIndex];
            for (int studentIndex = 0; studentIndex < orderedStudents.Count; studentIndex++)
            {
                var student = orderedStudents[studentIndex];
                var status = (studentIndex + lectureIndex) % 5 == 0 ? AttendanceStatus.Absent : AttendanceStatus.Present;
                context.Set<Attendance>().Add(new Attendance(student.Id, lecture.Id, status));
            }

            lecture.MarkHeld();
        }

        var futureLectures = await context.Set<Lecture>()
            .Where(l => l.LectureTime > now)
            .OrderBy(l => l.LectureTime)
            .Take(2)
            .ToListAsync();

        foreach (var lecture in futureLectures)
        {
            for (int i = 0; i < 4; i++)
            {
                context.Set<Attendance>().Add(new Attendance(orderedStudents[i].Id, lecture.Id, AttendanceStatus.Pending));
            }
        }

        await context.SaveChangesAsync();
    }

    public static async Task SeedAssignmentsAndSubmissions(ENoteContext context, IClock clock)
    {
        if (await context.Set<Assignment>().AnyAsync())
        {
            return;
        }

        var students = await SeedStudents.GetByUsernameAsync(context);
        if (students.Count < SeedStudents.Usernames.Length)
        {
            return;
        }

        List<Student> orderedStudents = [.. SeedStudents.Usernames.Select(u => students[u])];

        var now = clock.UtcNow;
        var pastLectures = await context.Set<Lecture>()
            .Where(l => l.LectureTime < now)
            .OrderBy(l => l.LectureTime)
            .ToListAsync();

        var closedAssignments = new List<Assignment>();
        for (int i = 0; i < pastLectures.Count; i++)
        {
            var lecture = pastLectures[i];
            var assignment = new Assignment(
                $"Zadaća: {lecture.Name}",
                $"Zadaci i vježbe vezane za predavanje '{lecture.Name}'.",
                lecture.LectureTime.AddDays(3),
                lecture.Id);
            closedAssignments.Add(assignment);
        }

        context.Set<Assignment>().AddRange(closedAssignments);

        var ritamLecture = await context.Set<Lecture>()
            .FirstOrDefaultAsync(l => l.Name == "Ritam i metrika");

        if (ritamLecture != null)
        {
            var openAssignment = new Assignment(
                "Praktični rad: Ritam i metrika",
                "Snimiti audio zapis zadane ritmičke vježbe.",
                now.AddDays(7),
                ritamLecture.Id);
            context.Set<Assignment>().Add(openAssignment);
        }

        await context.SaveChangesAsync();

        for (int assignmentIndex = 0; assignmentIndex < closedAssignments.Count; assignmentIndex++)
        {
            var assignment = closedAssignments[assignmentIndex];
            for (int studentIndex = 0; studentIndex < orderedStudents.Count; studentIndex++)
            {
                var student = orderedStudents[studentIndex];
                var submission = new AssignmentSubmission(assignment.Id, student.Id);
                submission.Submit(null, assignment.DueAt.AddDays(-1));
                int grade = 55 + (studentIndex * 7 + assignmentIndex * 13) % 46;
                string? feedback = (studentIndex + assignmentIndex) % 2 == 0 ? "Dobar rad, obrati pažnju na ritam." : null;
                submission.SetGrade(grade, feedback);
                context.Set<AssignmentSubmission>().Add(submission);
            }
        }

        await context.SaveChangesAsync();
    }

    public static async Task SeedLectureNotes(ENoteContext context, IClock clock)
    {
        if (await context.Set<LectureNote>().AnyAsync())
        {
            return;
        }

        var now = clock.UtcNow;
        var pastLectures = await context.Set<Lecture>()
            .Where(l => l.LectureTime < now)
            .OrderBy(l => l.LectureTime)
            .ToListAsync();

        foreach (var lecture in pastLectures)
        {
            context.Set<LectureNote>().Add(new LectureNote(
                $"Bilješke: {lecture.Name}",
                $"Pregled obrađenog gradiva za predavanje '{lecture.Name}'. Zabilježeni su ključni teorijski i praktični primjeri sa časa.",
                lecture.Id));
        }

        await context.SaveChangesAsync();
    }
}
