using eNote.Application.Common.Localization;

namespace eNote.Infrastructure.Reports;

internal static class ReportLabels
{
    public static string Rental(InstrumentRentalStatus status) => status switch
    {
        InstrumentRentalStatus.Pending => Messages.ReportRentalStatusPending,
        InstrumentRentalStatus.Approved => Messages.ReportRentalStatusApproved,
        InstrumentRentalStatus.Active => Messages.ReportRentalStatusActive,
        InstrumentRentalStatus.Completed => Messages.ReportRentalStatusCompleted,
        InstrumentRentalStatus.Rejected => Messages.ReportRentalStatusRejected,
        InstrumentRentalStatus.Canceled => Messages.ReportRentalStatusCanceled,
        InstrumentRentalStatus.ReturnedEarly => Messages.ReportRentalStatusReturnedEarly,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    public static string Attendance(AttendanceStatus status) => status switch
    {
        AttendanceStatus.Pending => Messages.ReportAttendanceStatusPending,
        AttendanceStatus.Present => Messages.ReportAttendanceStatusPresent,
        AttendanceStatus.Absent => Messages.ReportAttendanceStatusAbsent,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}
