using eNote.Application.Features.Identity.Users;
using FluentValidation;

namespace eNote.Application.Validation.Identity;

public sealed class DeleteProfileRequestValidator : AbstractValidator<DeleteProfileRequest>
{
    public DeleteProfileRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
    }
}
