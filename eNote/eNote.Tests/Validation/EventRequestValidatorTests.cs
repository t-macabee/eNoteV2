using eNote.Application.Common.Localization;
using eNote.Application.Features.Communication.Events;
using eNote.Application.Validation.Communication;

namespace eNote.Tests.Validation;

public sealed class EventRequestValidatorTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly EventRequestValidator _validator = new();

    [Fact]
    public void Validate_RejectsEndsBeforeStarts()
    {
        var result = _validator.Validate(new EventRequest
        {
            Title = "Bad Range",
            Description = "Invalid",
            StartsAt = Now.AddDays(2),
            EndsAt = Now.AddDays(1)
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(EventRequest.EndsAt) && e.ErrorMessage == Messages.EventEndsBeforeStarts);
    }

    [Fact]
    public void Validate_AcceptsEndsAfterStarts()
    {
        var result = _validator.Validate(new EventRequest
        {
            Title = "Valid Range",
            Description = "Valid",
            StartsAt = Now.AddDays(2),
            EndsAt = Now.AddDays(3)
        });

        Assert.True(result.IsValid);
    }
}
