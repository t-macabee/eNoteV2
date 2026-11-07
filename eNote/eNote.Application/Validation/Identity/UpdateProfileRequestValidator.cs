using eNote.Application.Features.Identity.Users;
using FluentValidation;

namespace eNote.Application.Validation.Identity;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator(IClock clock)
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(IdentityPolicy.EmailMaxLength);
        RuleFor(x => x.FirstName).NotEmpty().WithMessage(Messages.FirstNameRequired).MaximumLength(IdentityPolicy.PersonNameMaxLength).WithMessage(Messages.FirstNameTooLong);
        RuleFor(x => x.LastName).NotEmpty().WithMessage(Messages.LastNameRequired).MaximumLength(IdentityPolicy.PersonNameMaxLength).WithMessage(Messages.LastNameTooLong);
        RuleFor(x => x.DateOfBirth)
            .LessThanOrEqualTo(_ => clock.UtcNow.Date)
            .WithMessage(Messages.DateOfBirthInFuture)
            .When(x => x.DateOfBirth.HasValue);
    }
}
