using eNote.Application.Features.Identity.Students;
using FluentValidation;

namespace eNote.Application.Validation.Identity;

public sealed class RecordMembershipPaymentRequestValidator : AbstractValidator<RecordMembershipPaymentRequest>
{
    public RecordMembershipPaymentRequestValidator(IClock clock)
    {
        var today = clock.UtcNow.Date;
        var earliestDate = today.AddYears(-10);
        var latestDate = today.AddYears(10);

        RuleFor(x => x.Amount)
            .InclusiveBetween(0.01m, 99999999.99m).WithMessage(Messages.MembershipAmountInvalid);

        RuleFor(x => x.ReceiptNumber)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(Messages.MembershipReceiptNumberRequired)
            .Must(receiptNumber => receiptNumber.Trim().Length <= 50).WithMessage(Messages.MembershipReceiptNumberLength);

        RuleFor(x => x)
            .Must(x => x.PeriodEnd.HasValue ^ x.Months.HasValue)
            .WithMessage(Messages.MembershipPeriodSelectionRequired);

        RuleFor(x => x.Months)
            .InclusiveBetween(1, 24).WithMessage(Messages.MembershipMonthsRange)
            .When(x => x.Months.HasValue);

        RuleFor(x => x.PeriodEnd)
            .Must(periodEnd => periodEnd!.Value.Date >= today && periodEnd.Value.Date <= latestDate)
            .WithMessage(Messages.MembershipPeriodEndRange)
            .When(x => x.PeriodEnd.HasValue);

        RuleFor(x => x.PaidAt)
            .Must(paidAt => paidAt!.Value.Date >= earliestDate && paidAt.Value.Date <= today)
            .WithMessage(Messages.MembershipPaidAtRange)
            .When(x => x.PaidAt.HasValue);

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage(Messages.MembershipNoteTooLong);
    }
}
