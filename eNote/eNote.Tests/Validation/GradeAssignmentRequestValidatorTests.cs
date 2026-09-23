using eNote.Application.Common.Localization;
using eNote.Application.Features.Academic.Assignments;
using eNote.Application.Validation.Academic;

namespace eNote.Tests.Validation;

public sealed class GradeAssignmentRequestValidatorTests
{
    private readonly GradeAssignmentRequestValidator _validator = new();

    [Fact]
    public void Validate_AcceptsFeedbackUpTo1000Characters()
    {
        var request = new GradeAssignmentRequest
        {
            Grade = 80,
            Feedback = new string('a', 1000)
        };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_RejectsFeedbackOver1000Characters()
    {
        var request = new GradeAssignmentRequest
        {
            Grade = 80,
            Feedback = new string('a', 1001)
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GradeAssignmentRequest.Feedback));
        Assert.Contains(result.Errors, e => e.ErrorMessage == Messages.AssignmentFeedbackTooLong);
    }

    [Fact]
    public void Validate_AcceptsNullFeedback()
    {
        var request = new GradeAssignmentRequest
        {
            Grade = 80,
            Feedback = null
        };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
