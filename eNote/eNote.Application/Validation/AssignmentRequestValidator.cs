using eNote.Application.Features.Academic.Assignments;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class AssignmentRequestValidator : AbstractValidator<AssignmentRequest>
{
    public AssignmentRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.DueAt).NotEmpty();
    }
}
