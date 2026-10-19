using System.Text.Json;
using eNote.Application.Common.Files;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Communication.Events;
using eNote.Tests.TestUtils;
using Microsoft.Extensions.Logging.Abstractions;

namespace eNote.Tests.Storage;

public sealed class DefaultImageDtoTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CourseAndEventDtos_SerializeWithDefaultImagePath()
    {
        var harness = await AcademicTestData.SeedAsync(TestDbContextFactory.CreateContext(Now), Now);
        var actor = new StubCurrentActor(instructor: harness.Instructor, student: harness.Student);
        var courseService = new CourseService(
            harness.Context,
            TestMapper.Create(),
            actor,
            actor,
            AcademicTestData.CreateInstructorAccess(harness.Context, harness.Instructor),
            NullLogger<CourseService>.Instance,
            new StubUserIdentityService());

        var courseDto = await courseService.GetByIdForAdminAsync(harness.Course.Id);

        var eventDto = await new EventService(harness.Context, actor).CreateAsync(new EventRequest
        {
            Title = "Image check",
            Description = "Has the event default image",
            StartsAt = Now.AddDays(3)
        });

        Assert.Equal(DefaultImages.Course, ReadImagePath(courseDto));
        Assert.Equal(DefaultImages.Event, ReadImagePath(eventDto));
    }

    private static string? ReadImagePath<T>(T dto)
    {
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("imagePath").GetString();
    }
}
