using eNote.Application.Common.Time;
using eNote.Application.Features.Identity.Users;
using FluentValidation;

namespace eNote.Application.Validation.Identity;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator(IClock clock)
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FirstName).MaximumLength(256);
        RuleFor(x => x.LastName).MaximumLength(256);
        RuleFor(x => x.DateOfBirth)
            .LessThanOrEqualTo(_ => clock.UtcNow.Date)
            .When(x => x.DateOfBirth.HasValue);
    }
}
