using eNote.Application.Features.Academic.Assignments;
using FluentValidation;

namespace eNote.Application.Validation.Academic;

public sealed class AssignmentRequestValidator : AbstractValidator<AssignmentRequest>
{
    public AssignmentRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(Assignment.TitleMaxLength);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.DueAt).NotEmpty();
    }
}
