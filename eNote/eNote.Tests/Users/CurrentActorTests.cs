using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace eNote.Tests.Users;

public sealed class CurrentActorTests
{
    [Fact]
    public async Task GetStudentAsync_ResolvesStudentOnlyOnce()
    {
        var lookup = new CountingProfileLookup(new Student(appUserId: 42, enrollmentDate: DateTime.UtcNow));
        var actor = new CurrentActor(new TestCurrentUserService(42), lookup, new ThrowingDbContext());

        var first = await actor.GetStudentAsync();
        var second = await actor.GetStudentAsync();

        Assert.Same(first, second);
        Assert.Equal(1, lookup.StudentLookupCount);
    }

    private sealed class TestCurrentUserService(int userId) : ICurrentUserService
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

    private sealed class ThrowingDbContext : IAppDbContext
    {
        public DbSet<TEntity> Set<TEntity>() where TEntity : class => throw new NotSupportedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
