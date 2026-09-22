using eNote.Application.Features.Academic.LectureNotes;
using FluentValidation;

namespace eNote.Application.Validation.Academic;

public sealed class LectureNoteRequestValidator : AbstractValidator<LectureNoteRequest>
{
    public LectureNoteRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Content).NotEmpty();
    }
}
