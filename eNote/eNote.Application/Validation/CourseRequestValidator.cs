using eNote.Application.Features.Courses;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class CourseRequestValidator : AbstractValidator<CourseRequest>
{
    public CourseRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).WithMessage("Price must be non-negative.");
    }
}
