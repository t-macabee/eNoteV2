using eNote.Application.Common.Exceptions;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Tests.TestUtils;

namespace eNote.Tests.Identity;

public sealed class UserProfileLookupTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetStudentAsync_FindsTheProfileByUserId_NotByProfileId()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var first = new Student(501, Now);
        var second = new Student(502, Now);
        context.Set<Student>().AddRange(first, second);
        await context.SaveChangesAsync();
        var lookup = new UserProfileLookup(context);

        var found = await lookup.GetStudentAsync(502);

        Assert.Equal(second.Id, found.Id);
        Assert.NotEqual(502, found.Id);
    }

    [Fact]
    public async Task GetStudentAsync_Throws_WhenTheUserHasNoProfile()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        context.Set<Student>().Add(new Student(501, Now));
        await context.SaveChangesAsync();
        var lookup = new UserProfileLookup(context);

        await Assert.ThrowsAsync<BusinessException>(() => lookup.GetStudentAsync(999));
    }

    [Fact]
    public async Task GetInstructorAsync_FindsTheProfileByUserId_NotByProfileId()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var first = new Instructor(601);
        var second = new Instructor(602);
        context.Set<Instructor>().AddRange(first, second);
        await context.SaveChangesAsync();
        var lookup = new UserProfileLookup(context);

        var found = await lookup.GetInstructorAsync(602);

        Assert.Equal(second.Id, found.Id);
        Assert.NotEqual(602, found.Id);
    }

    [Fact]
    public async Task GetInstructorAsync_Throws_WhenTheUserHasNoProfile()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var lookup = new UserProfileLookup(context);

        await Assert.ThrowsAsync<BusinessException>(() => lookup.GetInstructorAsync(999));
    }

    [Fact]
    public async Task GetEmployeeAsync_FindsByUserId_AndThrowsWhenMissing()
    {
        await using var context = TestDbContextFactory.CreateContext(Now);
        var other = new MusicStoreEmployee(701, 1, false);
        var employee = new MusicStoreEmployee(702, 1, false);
        context.Set<MusicStoreEmployee>().AddRange(other, employee);
        await context.SaveChangesAsync();
        var lookup = new UserProfileLookup(context);

        var found = await lookup.GetEmployeeAsync(702);

        Assert.Equal(employee.Id, found.Id);
        await Assert.ThrowsAsync<BusinessException>(() => lookup.GetEmployeeAsync(999));
    }
}
