using eNote.Application.Features.Identity.Users.Services;
using eNote.Tests.TestUtils;

namespace eNote.Tests.Identity;

public sealed class CurrentActorTests
{
    [Fact]
    public async Task GetStudentAsync_ResolvesStudentOnlyOnce()
    {
        var lookup = new CountingProfileLookup(new Student(appUserId: 42, enrollmentDate: DateTime.UtcNow));
        var actor = new CurrentActor(new StubCurrentActor(userId: 42), lookup);

        var first = await actor.GetCurrentStudentAsync();
        var second = await actor.GetCurrentStudentAsync();

        Assert.Same(first, second);
        Assert.Equal(1, lookup.StudentLookupCount);
    }

    private sealed class CountingProfileLookup(Student student) : IUserProfileLookup
    {
        public int StudentLookupCount { get; private set; }

        public Task<Student> GetStudentAsync(int userId, CancellationToken cancellationToken = default)
        {
            StudentLookupCount++;
            return Task.FromResult(student);
        }

        public Task<Instructor> GetInstructorAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<MusicStoreEmployee> GetActiveEmployeeAsync(int userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
