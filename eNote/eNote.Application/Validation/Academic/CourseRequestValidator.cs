using eNote.Application.Features.Academic.Courses;
using FluentValidation;

namespace eNote.Application.Validation.Academic;

public sealed class CourseRequestValidator : AbstractValidator<CourseRequest>
{
    public CourseRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Naziv kursa je obavezan.").MaximumLength(200).WithMessage("Naziv kursa ne smije biti duži od 200 karaktera.");
        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Cijena mora biti nenegativan decimalni broj (npr. 25.00).");
        RuleFor(x => x.Price)
            .LessThan(10000)
            .WithMessage("Cijena ne smije biti veća od 10000.");
    }
}
