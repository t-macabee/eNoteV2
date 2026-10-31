using eNote.Application.Features.Communication.Announcements;
using FluentValidation;

namespace eNote.Application.Validation.Communication;

public sealed class AnnouncementRequestValidator : AbstractValidator<AnnouncementRequest>
{
    public AnnouncementRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(Announcement.TitleMaxLength);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(Announcement.ContentMaxLength);
    }
}
