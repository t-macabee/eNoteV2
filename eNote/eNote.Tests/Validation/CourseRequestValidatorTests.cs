using eNote.Application.Features.Academic.Courses;
using eNote.Application.Validation.Academic;

namespace eNote.Tests.Validation;

public sealed class CourseRequestValidatorTests
{
    private readonly CourseRequestValidator _validator = new();

    [Fact]
    public void Validate_AcceptsNameOf200Characters()
    {
        var request = ValidRequest();
        request.Name = new string('a', 200);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_RejectsNameOf201Characters()
    {
        var request = ValidRequest();
        request.Name = new string('a', 201);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CourseRequest.Name) && e.ErrorMessage == "Naziv kursa ne smije biti duži od 200 karaktera.");
    }

    private static CourseRequest ValidRequest() => new()
    {
        Name = "Gitara za početnike",
        Description = "Osnovni kurs gitare.",
        Price = 120m
    };
}
