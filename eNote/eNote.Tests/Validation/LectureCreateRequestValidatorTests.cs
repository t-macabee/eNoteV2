using eNote.Application.Common.Localization;
using eNote.Application.Features.Academic.Lectures;
using eNote.Application.Validation.Academic;

namespace eNote.Tests.Validation;

public sealed class LectureCreateRequestValidatorTests
{
    private readonly LectureCreateRequestValidator _validator = new();

    [Fact]
    public void Validate_RejectsMissingCourseId()
    {
        var request = ValidRequest();

        request.CourseId = 0;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LectureCreateRequest.CourseId));
        Assert.Contains(result.Errors, e => e.ErrorMessage == Messages.CourseIdRequired);
    }

    [Fact]
    public void Validate_AcceptsRequestWithCourseId()
    {
        var result = _validator.Validate(ValidRequest());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    private static LectureCreateRequest ValidRequest() => new()
    {
        CourseId = 1,
        Name = "Uvod",
        Location = "Amfiteatar",
        LectureType = LectureType.Theoretical,
        LectureTime = new DateTime(2026, 9, 1, 18, 0, 0),
        Duration = 90
    };
}
