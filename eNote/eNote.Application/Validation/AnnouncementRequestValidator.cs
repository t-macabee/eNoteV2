using eNote.Application.Features.Communication.Announcements;
using FluentValidation;

namespace eNote.Application.Validation;

public sealed class AnnouncementRequestValidator : AbstractValidator<AnnouncementRequest>
{
    public AnnouncementRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.Content).NotEmpty();
    }
}
