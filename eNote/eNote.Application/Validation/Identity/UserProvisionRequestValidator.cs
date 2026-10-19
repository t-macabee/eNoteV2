using eNote.Application.Constants;
using eNote.Application.Features.Identity.Users;
using FluentValidation;

namespace eNote.Application.Validation.Identity;

public sealed class UserProvisionRequestValidator : AbstractValidator<UserProvisionRequest>
{
    public UserProvisionRequestValidator()
    {
        Include(new DelegatedUserCreateRequestValidator());

        RuleFor(x => x.Role).NotEmpty().Must(BeKnownRole).WithMessage(Messages.UnknownRole);
        RuleFor(x => x.MusicStoreId)
            .NotNull().WithMessage(Messages.MusicStoreRequiredForEmployee)
            .When(x => x.Role == AppRoles.StoreEmployee);
        RuleFor(x => x.MusicStoreId)
            .GreaterThan(0).WithMessage(Messages.MusicStoreSelectionRequired)
            .When(x => x.MusicStoreId.HasValue);
    }

    private static bool BeKnownRole(string role) =>
        role is AppRoles.Administrator or AppRoles.Instructor or AppRoles.Student or AppRoles.StoreEmployee;
}
