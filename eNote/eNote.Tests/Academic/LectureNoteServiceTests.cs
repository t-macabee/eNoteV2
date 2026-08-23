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
        var service = CreateService(harness.Context, harness.Instructor);

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
        var service = CreateService(harness.Context, otherInstructor);

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
        var service = CreateService(harness.Context, harness.Instructor);

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
        var service = CreateService(harness.Context, harness.Instructor);

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
        var service = CreateService(harness.Context, harness.Instructor, new StubCurrentActor(student: harness.Student));

        var result = await service.GetForStudentAsync(harness.Lecture.Id, new LectureNoteSearchObject { Page = 1, PageSize = 10 });

        var dto = Assert.Single(result.Items);
        Assert.Equal("Visible", dto.Title);
    }

    private static LectureNoteService CreateService(ENoteContext context, Instructor instructor, StubCurrentActor? actor = null)
    {
        var currentUser = actor ?? new StubCurrentActor(instructor: instructor);
        return new(context,
            currentUser,
            currentUser,
            AcademicTestData.CreateInstructorAccess(context, instructor),
            TestMapper.Create());
    }
}
