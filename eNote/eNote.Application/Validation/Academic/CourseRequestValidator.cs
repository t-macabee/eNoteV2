using eNote.Application.Features.Academic.Courses;
using FluentValidation;

namespace eNote.Application.Validation.Academic;

public sealed class CourseRequestValidator : AbstractValidator<CourseRequest>
{
    public CourseRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Naziv kursa je obavezan.").MaximumLength(Course.NameMaxLength).WithMessage($"Naziv kursa ne smije biti duži od {Course.NameMaxLength} karaktera.");
        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0)
            .WithMessage(Messages.CoursePriceMustBeNonNegative)
            .LessThanOrEqualTo(10000)
            .WithMessage(Messages.CoursePriceExceedsMaximum);
        RuleFor(x => x.EndDate)
            .Must((request, endDate) => !endDate.HasValue || !request.StartDate.HasValue || endDate >= request.StartDate)
            .WithMessage("Datum završetka ne smije biti prije datuma početka.");
    }
}
