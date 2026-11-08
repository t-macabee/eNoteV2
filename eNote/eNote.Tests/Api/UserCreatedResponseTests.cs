using eNote.API.Controllers.Academic;
using eNote.Application.Constants;
using eNote.Application.Features.Identity.Auth;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Students;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Tests.TestUtils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Reflection;

namespace eNote.Tests.Api;

public sealed class UserCreatedResponseTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task PostInstructorStudents_ReturnsAUserCreatedResponseBody()
    {
        await using var app = CreateApp();
        await app.StartAsync();

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
            using var response = await client.PostAsJsonAsync("/api/v1/instructor/students", new DelegatedUserCreateRequest
            {
                Username = "newstudent",
                Email = "student@example.com",
                Password = "Password1!",
                FirstName = "Test",
                LastName = "User"
            });

            Assert.Equal(201, (int)response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<UserCreatedResponse>();
            Assert.NotNull(body);
            Assert.Equal(42, body.UserId);

            var declaredType = typeof(InstructorStudentController)
                .GetMethod(nameof(InstructorStudentController.Create))!
                .GetCustomAttributes<ProducesResponseTypeAttribute>()
                .Single(a => a.StatusCode == StatusCodes.Status201Created)
                .Type;

            Assert.Equal(typeof(UserCreatedResponse), declaredType);
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private static WebApplication CreateApp()
    {
        var context = TestDbContextFactory.CreateContext(Now);
        var instructor = new Instructor(100);
        var actor = new StubCurrentActor(userId: instructor.AppUserId);
        var instructorAccess = new InstructorAccessService(context, new StubUserProfileLookup(instructor: instructor));
        var studentService = new AdminStudentService(context, new StubUserIdentityService(), instructorAccess, actor);
        var membershipService = new MembershipService(
            context,
            studentService,
            new FixedClock(Now),
            new RecordingMembershipWarningDispatcher(),
            actor,
            instructorAccess);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddControllers().AddApplicationPart(typeof(InstructorStudentController).Assembly);
        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
            options.ApiVersionReader = new Asp.Versioning.UrlSegmentApiVersionReader();
        }).AddMvc();
        builder.Services.AddAuthentication(TestAuthenticationHandler.SchemeName)
            .AddScheme<TestAuthenticationOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, options =>
            {
                options.Role = AppRoles.Instructor;
                options.UserId = instructor.AppUserId;
            });
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton(studentService);
        builder.Services.AddSingleton(membershipService);
        builder.Services.AddSingleton<IUserProvisioningService>(new StubProvisioningService { CreatedUserId = 42 });

        var app = builder.Build();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

    private sealed class StubProvisioningService : IUserProvisioningService
    {
        public int CreatedUserId { get; set; }

        public Task<RegistrationResult> RegisterStudentAsync(RegisterRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> ProvisionUserAsync(UserProvisionRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(CreatedUserId);

        public Task SetUserActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteUserAsync(int userId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> IsStoreManagerAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<int> ProvisionStudentByInstructorAsync(DelegatedUserCreateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(CreatedUserId);

        public Task<int> ProvisionEmployeeByManagerAsync(DelegatedUserCreateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(CreatedUserId);
    }
}
