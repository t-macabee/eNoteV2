using eNote.Application.Features.Academic.Lectures;
using FluentValidation;

namespace eNote.Application.Validation.Academic;

public sealed class LectureCreateRequestValidator : AbstractValidator<LectureCreateRequest>
{
    public LectureCreateRequestValidator()
    {
        Include(new LectureUpdateRequestValidator());

        RuleFor(x => x.CourseId).GreaterThan(0).WithMessage(Messages.CourseIdRequired);
    }
}
