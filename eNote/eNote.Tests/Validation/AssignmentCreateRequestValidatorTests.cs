using eNote.Application.Common.Localization;
using eNote.Application.Features.Academic.Assignments;
using eNote.Application.Validation.Academic;
using eNote.Tests.TestUtils;

namespace eNote.Tests.Validation;

public sealed class AssignmentCreateRequestValidatorTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly AssignmentCreateRequestValidator _validator = new(new FixedClock(Now));

    [Fact]
    public void Validate_RejectsDueAtInThePast()
    {
        var request = ValidRequest();
        request.DueAt = Now.AddMinutes(-1);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AssignmentCreateRequest.DueAt) && e.ErrorMessage == Messages.AssignmentDueAtMustBeFuture);
    }

    [Fact]
    public void Validate_RejectsDueAtEqualToCurrentTime()
    {
        var request = ValidRequest();
        request.DueAt = Now;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AssignmentCreateRequest.DueAt) && e.ErrorMessage == Messages.AssignmentDueAtMustBeFuture);
    }

    [Fact]
    public void Validate_AcceptsDueAtInTheFuture()
    {
        var result = _validator.Validate(ValidRequest());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_KeepsAssignmentRequestRules()
    {
        var request = ValidRequest();
        request.Title = " ";

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AssignmentCreateRequest.Title));
    }

    private static AssignmentCreateRequest ValidRequest() => new()
    {
        Title = "Domaći zadatak",
        Description = "Uradite vježbe",
        DueAt = Now.AddDays(7)
    };
}
