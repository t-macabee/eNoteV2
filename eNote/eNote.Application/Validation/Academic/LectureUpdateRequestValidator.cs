using eNote.Application.Features.Academic.Lectures;
using FluentValidation;

namespace eNote.Application.Validation.Academic;

public sealed class LectureUpdateRequestValidator : AbstractValidator<LectureUpdateRequest>
{
    public LectureUpdateRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Location).NotEmpty();
        RuleFor(x => x.LectureTime).NotEmpty();
        RuleFor(x => x.Duration).GreaterThan(0);
        RuleFor(x => x.Capacity).GreaterThan(0).When(x => x.Capacity.HasValue);
    }
}
