using eNote.Application.Features.Academic.LectureNotes;
using eNote.Application.Features.Academic.LectureNotes.Services;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Academic;

public sealed class LectureNoteServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateAsync_CreatesNote_ForOwnedLecture()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var service = AcademicTestData.CreateService<LectureNoteService>(harness.Context, harness.Instructor);

        var dto = await service.CreateAsync(harness.Lecture.Id, new LectureNoteRequest { Title = "Scales", Content = "A minor pentatonic" });

        Assert.Equal("Scales", dto.Title);
        Assert.Equal(harness.Lecture.Id, dto.LectureId);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenLectureNotOwned()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var otherInstructor = new Instructor(300);
        harness.Context.Set<Instructor>().Add(otherInstructor);
        await harness.Context.SaveChangesAsync();
        var service = AcademicTestData.CreateService<LectureNoteService>(harness.Context, otherInstructor);

        await Assert.ThrowsAsync<AuthorizationException>(() =>
            service.CreateAsync(harness.Lecture.Id, new LectureNoteRequest { Title = "Scales", Content = "Content" }));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOwnedNote()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var note = new LectureNote("Scales", "Old", harness.Lecture.Id);
        harness.Context.Set<LectureNote>().Add(note);
        await harness.Context.SaveChangesAsync();
        var service = AcademicTestData.CreateService<LectureNoteService>(harness.Context, harness.Instructor);

        var dto = await service.UpdateAsync(harness.Lecture.Id, note.Id, new LectureNoteRequest { Title = "Scales v2", Content = "New" });

        Assert.Equal("Scales v2", dto.Title);
        Assert.Equal("New", dto.Content);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesOwnedNote()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var note = new LectureNote("Scales", "Content", harness.Lecture.Id);
        harness.Context.Set<LectureNote>().Add(note);
        await harness.Context.SaveChangesAsync();
        var service = AcademicTestData.CreateService<LectureNoteService>(harness.Context, harness.Instructor);

        await service.DeleteAsync(harness.Lecture.Id, note.Id);

        var row = await harness.Context.Set<LectureNote>().AsNoTracking().IgnoreQueryFilters().SingleAsync();
        Assert.False(row.IsActive);
    }

    [Fact]
    public async Task GetForStudentAsync_ReturnsOnlyEnrolledLectureNotes()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        harness.Context.Set<LectureNote>().Add(new LectureNote("Visible", "Content", harness.Lecture.Id));
        harness.Context.Set<LectureNote>().Add(new LectureNote("Hidden", "Content", 9999));
        await harness.Context.SaveChangesAsync();
        var service = AcademicTestData.CreateService<LectureNoteService>(harness.Context, harness.Instructor, new StubCurrentActor(student: harness.Student));

        var result = await service.GetForStudentAsync(harness.Lecture.Id, new LectureNoteSearchObject { Page = 1, PageSize = 10 });

        var dto = Assert.Single(result.Items);
        Assert.Equal("Visible", dto.Title);
    }

    [Fact]
    public async Task GetForStudentAsync_ReturnsNotes_AfterCourseUnpublished_WhenEnrolledAndPaid()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        harness.Context.Set<LectureNote>().Add(new LectureNote("Visible", "Content", harness.Lecture.Id));
        harness.Context.Set<Course>().Single(c => c.Id == harness.Course.Id).SetPublishedStatus(false);
        await harness.Context.SaveChangesAsync();
        var service = AcademicTestData.CreateService<LectureNoteService>(harness.Context, harness.Instructor, new StubCurrentActor(student: harness.Student));

        var result = await service.GetForStudentAsync(harness.Lecture.Id, new LectureNoteSearchObject { Page = 1, PageSize = 10 });

        var dto = Assert.Single(result.Items);
        Assert.Equal("Visible", dto.Title);
    }


    [Fact]
    public async Task GetForStudentAsync_HidesNotesOfClassmatesIndividualLecture()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        harness.Context.Set<LectureNote>().Add(new LectureNote("Group note", "Content", harness.Lecture.Id));
        var classmate = await AcademicTestData.AddEnrolledStudentAsync(harness, 60, Now);
        var classmateLecture = await AcademicTestData.AddIndividualLectureAsync(harness, classmate, Now.AddDays(2));
        harness.Context.Set<LectureNote>().Add(new LectureNote("Classmate plan", "Content", classmateLecture.Id));
        await harness.Context.SaveChangesAsync();
        var service = AcademicTestData.CreateService<LectureNoteService>(harness.Context, harness.Instructor, new StubCurrentActor(student: harness.Student));

        var groupNotes = await service.GetForStudentAsync(harness.Lecture.Id, new LectureNoteSearchObject { Page = 1, PageSize = 10 });
        var classmateNotes = await service.GetForStudentAsync(classmateLecture.Id, new LectureNoteSearchObject { Page = 1, PageSize = 10 });

        Assert.Equal("Group note", Assert.Single(groupNotes.Items).Title);
        Assert.Empty(classmateNotes.Items);
    }

    [Fact]
    public async Task GetByIdForStudentAsync_Throws_ForClassmatesIndividualLectureNote()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var classmate = await AcademicTestData.AddEnrolledStudentAsync(harness, 60, Now);
        var classmateLecture = await AcademicTestData.AddIndividualLectureAsync(harness, classmate, Now.AddDays(2));
        var note = new LectureNote("Classmate plan", "Content", classmateLecture.Id);
        harness.Context.Set<LectureNote>().Add(note);
        await harness.Context.SaveChangesAsync();
        var service = AcademicTestData.CreateService<LectureNoteService>(harness.Context, harness.Instructor, new StubCurrentActor(student: harness.Student));

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdForStudentAsync(classmateLecture.Id, note.Id));
    }

    [Fact]
    public async Task GetForStudentAsync_ReturnsNotesOfOwnIndividualLecture()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var ownLecture = await AcademicTestData.AddIndividualLectureAsync(harness, harness.Student, Now.AddDays(2));
        harness.Context.Set<LectureNote>().Add(new LectureNote("Moj plan", "Content", ownLecture.Id));
        await harness.Context.SaveChangesAsync();
        var service = AcademicTestData.CreateService<LectureNoteService>(harness.Context, harness.Instructor, new StubCurrentActor(student: harness.Student));

        var result = await service.GetForStudentAsync(ownLecture.Id, new LectureNoteSearchObject { Page = 1, PageSize = 10 });

        Assert.Equal("Moj plan", Assert.Single(result.Items).Title);
    }
}
