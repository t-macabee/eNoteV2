using eNote.Application.Common.Localization;
using eNote.Application.Features.Academic.Lectures;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class LectureCreateRequestValidator : AbstractValidator<LectureCreateRequest>
{
    public LectureCreateRequestValidator()
    {
        RuleFor(x => x.CourseId).GreaterThan(0).WithMessage(Messages.CourseIdRequired);
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Location).NotEmpty();
        RuleFor(x => x.LectureTime).NotEmpty();
        RuleFor(x => x.Duration).GreaterThan(0);
    }
}
