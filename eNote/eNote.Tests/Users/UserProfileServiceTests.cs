using eNote.Application.Common.Persistence;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Profiles;
using eNote.Application.Features.Identity.Users.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace eNote.Tests.Users;

public sealed class UserProfileServiceTests
{
    [Fact]
    public async Task GetCurrentUserAsync_UsesCurrentUserId()
    {
        var identity = new StubUserIdentityService
        {
            User = ActiveUser(15),
            Roles = [AppRoles.Administrator]
        };

        var service = CreateService(identity, currentUserId: 15);

        var result = await service.GetCurrentUserAsync();

        Assert.NotNull(result);
        Assert.Equal(15, identity.LastRequestedUserId);
        Assert.Equal(AppRoles.Administrator, result.Role);
    }

    [Fact]
    public async Task GetUserAsync_ReturnsNull_WhenUserIsInactive()
    {
        var identity = new StubUserIdentityService
        {
            User = new UserIdentityDto
            {
                Id = 1,
                Username = "inactive",
                IsActive = false
            },
            Roles = [AppRoles.Administrator]
        };

        var service = CreateService(identity);

        var result = await service.GetUserAsync(1);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserAsync_Throws_WhenUserHasMultipleRoles()
    {
        var identity = new StubUserIdentityService
        {
            User = ActiveUser(1),
            Roles = [AppRoles.Student, AppRoles.Instructor]
        };

        var service = CreateService(identity);

        await Assert.ThrowsAsync<BusinessException>(() => service.GetUserAsync(1));
    }

    [Fact]
    public async Task GetUserAsync_BuildsAdminProfile()
    {
        var identity = new StubUserIdentityService
        {
            User = new UserIdentityDto
            {
                Id = 3,
                Username = "admin",
                FirstName = "Admin",
                LastName = "User",
                IsActive = true
            },
            Roles = [AppRoles.Administrator]
        };

        var service = CreateService(identity);

        var result = await service.GetUserAsync(3);

        Assert.NotNull(result);
        Assert.Equal(AppRoles.Administrator, result.Role);
        var profile = Assert.IsType<AdminProfile>(result.Profile);
        Assert.Equal("Admin", profile.FirstName);
        Assert.Equal("User", profile.LastName);
    }

    private static UserProfileService CreateService(StubUserIdentityService identity, int currentUserId = 1) =>
        new(new ThrowingDbContext(), identity, new ThrowingUserProfileLookup(), new TestCurrentUserService(currentUserId));

    private static UserIdentityDto ActiveUser(int id) => new()
    {
        Id = id,
        Username = $"user{id}",
        IsActive = true
    };

    private sealed class TestCurrentUserService(int userId) : ICurrentUserService
    {
        public int UserId => userId;
        public bool IsAuthenticated => true;
    }

    private sealed class StubUserIdentityService : IUserIdentityService
    {
        public UserIdentityDto? User { get; init; }
        public IReadOnlyList<string> Roles { get; init; } = [];
        public int? LastRequestedUserId { get; private set; }

        public Task<UserIdentityDto?> GetUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            LastRequestedUserId = userId;
            return Task.FromResult(User);
        }

        public Task<IReadOnlyDictionary<int, UserIdentityDto>> GetUsersBulkAsync(IEnumerable<int> userIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<int, UserIdentityDto>>(new Dictionary<int, UserIdentityDto>());

        public Task<IReadOnlyList<string>> GetRolesAsync(int userId) => Task.FromResult(Roles);
    }

    private sealed class ThrowingUserProfileLookup : IUserProfileLookup
    {
        public Task<Student> GetStudentAsync(int userId) => throw new NotSupportedException();
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
