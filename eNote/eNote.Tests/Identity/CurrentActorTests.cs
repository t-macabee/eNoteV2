using eNote.Application.Features.Identity.Users.Services;

namespace eNote.Tests.Identity;

public sealed class CurrentActorTests
{
    [Fact]
    public async Task GetStudentAsync_ResolvesStudentOnlyOnce()
    {
        var lookup = new CountingProfileLookup(new Student(appUserId: 42, enrollmentDate: DateTime.UtcNow));
        var actor = new CurrentActor(new TestCurrentUserContext(42), lookup);

        var first = await actor.GetCurrentStudentAsync();
        var second = await actor.GetCurrentStudentAsync();

        Assert.Same(first, second);
        Assert.Equal(1, lookup.StudentLookupCount);
    }

    private sealed class TestCurrentUserContext(int userId) : ICurrentUserContext
    {
        public int UserId => userId;
        public bool IsAuthenticated => true;
    }

    private sealed class CountingProfileLookup(Student student) : IUserProfileLookup
    {
        public int StudentLookupCount { get; private set; }

        public Task<Student> GetStudentAsync(int userId)
        {
            StudentLookupCount++;
            return Task.FromResult(student);
        }

        public Task<Instructor> GetInstructorAsync(int userId) => throw new NotSupportedException();
        public Task<MusicStoreEmployee> GetActiveEmployeeAsync(int userId) => throw new NotSupportedException();
    }
}
