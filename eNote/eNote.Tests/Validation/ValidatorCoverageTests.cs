using eNote.Application.Features.Academic.Assignments;
using eNote.Application.Features.Academic.Courses;
using eNote.Application.Features.Academic.LectureNotes;
using eNote.Application.Features.Academic.Lectures;
using eNote.Application.Features.Communication.Announcements;
using eNote.Application.Features.Identity.Auth;
using eNote.Application.Features.Identity.Users;
using eNote.Application.Features.Rentals.InstrumentRentals;
using eNote.Application.Features.Rentals.Instruments;
using eNote.Application.Features.Rentals.ReferenceData.Addresses;
using eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;
using eNote.Application.Features.Rentals.ReferenceData.MusicStores;
using eNote.Application.Validation.Academic;
using eNote.Application.Validation.Communication;
using eNote.Application.Validation.Identity;
using eNote.Application.Validation.Rentals;
using FluentValidation;
using FluentValidation.Results;

namespace eNote.Tests.Validation;

public sealed class ValidatorCoverageTests
{
    [Fact]
    public void AllValidators_AcceptValidRequests()
    {
        Assert.True(new LoginRequestValidator().Validate(new LoginRequest { Username = "student", Password = "password" }).IsValid);
        Assert.True(new RegisterRequestValidator().Validate(new RegisterRequest { Username = "student", Email = "student@example.com", Password = "password1" }).IsValid);
        Assert.True(new ForgotPasswordRequestValidator().Validate(new ForgotPasswordRequest { Email = "student@example.com" }).IsValid);
        Assert.True(new ResetPasswordRequestValidator().Validate(new ResetPasswordRequest { Email = "student@example.com", Token = "token", NewPassword = "password1" }).IsValid);
        Assert.True(new ChangePasswordRequestValidator().Validate(new ChangePasswordRequest { CurrentPassword = "oldpassword", NewPassword = "password1", ConfirmNewPassword = "password1" }).IsValid);
        Assert.True(new UpdateProfileRequestValidator().Validate(new UpdateProfileRequest { Email = "student@example.com" }).IsValid);
        Assert.True(new UserProvisionRequestValidator().Validate(ValidUserProvision()).IsValid);
        Assert.True(new UpdateMembershipRequestValidator().Validate(new UpdateMembershipRequest { PaidUntil = DateTime.UtcNow.AddDays(1) }).IsValid);
        Assert.True(new UpdateMembershipRequestValidator().Validate(new UpdateMembershipRequest { PaidUntil = null }).IsValid);
        Assert.True(new InstrumentUpdateRequestValidator().Validate(new InstrumentUpdateRequest { Model = "Model", Manufacturer = "Maker", InstrumentTypeId = 1 }).IsValid);
        Assert.True(new InstrumentUpdateRequestValidator().Validate(new InstrumentUpdateRequest()).IsValid);
        Assert.True(new RsvpRequestValidator().Validate(new RsvpRequest { Confirm = true, Note = new string('x', 500) }).IsValid);
        Assert.True(new MarkAttendanceRequestValidator().Validate(new MarkAttendanceRequest { StudentId = 1, AttendanceStatus = AttendanceStatus.Present }).IsValid);
        Assert.True(new RentalCreateRequestValidator().Validate(new RentalCreateRequest { InstrumentId = 1 }).IsValid);
        Assert.True(new InstrumentCreateRequestValidator().Validate(new InstrumentCreateRequest { Model = "Model", Manufacturer = "Maker", InstrumentTypeId = 1 }).IsValid);
        Assert.True(new CourseRequestValidator().Validate(new CourseRequest { Name = "Course", Price = 0 }).IsValid);
        Assert.True(new AssignmentRequestValidator().Validate(new AssignmentRequest { Title = "Title", Description = "Description", DueAt = DateTime.UtcNow }).IsValid);
        Assert.True(new GradeAssignmentRequestValidator().Validate(new GradeAssignmentRequest { Grade = 100 }).IsValid);
        Assert.True(new LectureCreateRequestValidator().Validate(ValidLectureCreate()).IsValid);
        Assert.True(new LectureUpdateRequestValidator().Validate(ValidLectureUpdate()).IsValid);
        Assert.True(new LectureNoteRequestValidator().Validate(new LectureNoteRequest { Title = "Title", Content = "Content" }).IsValid);
        Assert.True(new InstrumentTypeRequestValidator().Validate(new InstrumentTypeRequest { Type = "Guitar", MonthlyFee = 0 }).IsValid);
        Assert.True(new MusicStoreRequestValidator().Validate(new MusicStoreRequest { StoreName = "Store", BusinessHours = "08-16" }).IsValid);
        Assert.True(new AddressRequestValidator().Validate(new AddressRequest { City = "Sarajevo", Street = "Street", Number = "1" }).IsValid);
        Assert.True(new AnnouncementRequestValidator().Validate(new AnnouncementRequest("Title", "Content")).IsValid);
    }

    [Fact]
    public void IdentityValidators_RejectMissingAndBoundaryValues()
    {
        AssertInvalid(new LoginRequestValidator().Validate(new LoginRequest { Username = "", Password = "password" }), nameof(LoginRequest.Username));
        AssertInvalid(new LoginRequestValidator().Validate(new LoginRequest { Username = "student", Password = "" }), nameof(LoginRequest.Password));
        AssertInvalid(new RegisterRequestValidator().Validate(new RegisterRequest { Username = "", Email = "student@example.com", Password = "password1" }), nameof(RegisterRequest.Username));
        AssertInvalid(new RegisterRequestValidator().Validate(new RegisterRequest { Username = "student", Email = "bad", Password = "password1" }), nameof(RegisterRequest.Email));
        AssertInvalid(new RegisterRequestValidator().Validate(new RegisterRequest { Username = "student", Email = "student@example.com", Password = "short" }), nameof(RegisterRequest.Password));
        AssertInvalid(new ForgotPasswordRequestValidator().Validate(new ForgotPasswordRequest { Email = "" }), nameof(ForgotPasswordRequest.Email));
        AssertInvalid(new ForgotPasswordRequestValidator().Validate(new ForgotPasswordRequest { Email = "bad" }), nameof(ForgotPasswordRequest.Email));
        AssertInvalid(new ResetPasswordRequestValidator().Validate(new ResetPasswordRequest { Email = "", Token = "token", NewPassword = "password1" }), nameof(ResetPasswordRequest.Email));
        AssertInvalid(new ResetPasswordRequestValidator().Validate(new ResetPasswordRequest { Email = "student@example.com", Token = "", NewPassword = "password1" }), nameof(ResetPasswordRequest.Token));
        AssertInvalid(new ResetPasswordRequestValidator().Validate(new ResetPasswordRequest { Email = "student@example.com", Token = "token", NewPassword = "short" }), nameof(ResetPasswordRequest.NewPassword));
        AssertInvalid(new ChangePasswordRequestValidator().Validate(new ChangePasswordRequest { CurrentPassword = "", NewPassword = "password1", ConfirmNewPassword = "password1" }), nameof(ChangePasswordRequest.CurrentPassword));
        AssertInvalid(new ChangePasswordRequestValidator().Validate(new ChangePasswordRequest { CurrentPassword = "oldpassword", NewPassword = "short", ConfirmNewPassword = "short" }), nameof(ChangePasswordRequest.NewPassword));
        AssertInvalid(new ChangePasswordRequestValidator().Validate(new ChangePasswordRequest { CurrentPassword = "oldpassword", NewPassword = "password1", ConfirmNewPassword = "different" }), nameof(ChangePasswordRequest.ConfirmNewPassword));
        AssertInvalid(new UpdateProfileRequestValidator().Validate(new UpdateProfileRequest { Email = "" }), nameof(UpdateProfileRequest.Email));
        AssertInvalid(new UpdateProfileRequestValidator().Validate(new UpdateProfileRequest { Email = "bad" }), nameof(UpdateProfileRequest.Email));
    }

    [Fact]
    public void AcademicValidators_RejectMissingAndBoundaryValues()
    {
        AssertInvalid(new CourseRequestValidator().Validate(new CourseRequest { Name = "", Price = 0 }), nameof(CourseRequest.Name));
        AssertInvalid(new CourseRequestValidator().Validate(new CourseRequest { Name = "Course", Price = -0.01m }), nameof(CourseRequest.Price));
        AssertInvalid(new AssignmentRequestValidator().Validate(new AssignmentRequest { Title = "", Description = "Description", DueAt = DateTime.UtcNow }), nameof(AssignmentRequest.Title));
        AssertInvalid(new AssignmentRequestValidator().Validate(new AssignmentRequest { Title = "Title", Description = "", DueAt = DateTime.UtcNow }), nameof(AssignmentRequest.Description));
        AssertInvalid(new AssignmentRequestValidator().Validate(new AssignmentRequest { Title = "Title", Description = "Description", DueAt = default }), nameof(AssignmentRequest.DueAt));
        AssertInvalid(new GradeAssignmentRequestValidator().Validate(new GradeAssignmentRequest { Grade = -1 }), nameof(GradeAssignmentRequest.Grade));
        AssertInvalid(new GradeAssignmentRequestValidator().Validate(new GradeAssignmentRequest { Grade = 101 }), nameof(GradeAssignmentRequest.Grade));
        AssertInvalid(new LectureCreateRequestValidator().Validate(new LectureCreateRequest { CourseId = 0, Name = "Name", Location = "Room", LectureTime = DateTime.UtcNow, Duration = 60 }), nameof(LectureCreateRequest.CourseId));
        AssertInvalid(new LectureCreateRequestValidator().Validate(new LectureCreateRequest { CourseId = 1, Name = "", Location = "Room", LectureTime = DateTime.UtcNow, Duration = 60 }), nameof(LectureCreateRequest.Name));
        AssertInvalid(new LectureCreateRequestValidator().Validate(new LectureCreateRequest { CourseId = 1, Name = "Name", Location = "", LectureTime = DateTime.UtcNow, Duration = 60 }), nameof(LectureCreateRequest.Location));
        AssertInvalid(new LectureCreateRequestValidator().Validate(new LectureCreateRequest { CourseId = 1, Name = "Name", Location = "Room", LectureTime = default, Duration = 60 }), nameof(LectureCreateRequest.LectureTime));
        AssertInvalid(new LectureCreateRequestValidator().Validate(new LectureCreateRequest { CourseId = 1, Name = "Name", Location = "Room", LectureTime = DateTime.UtcNow, Duration = 0 }), nameof(LectureCreateRequest.Duration));
        AssertInvalid(new LectureUpdateRequestValidator().Validate(new LectureUpdateRequest { Name = "", Location = "Room", LectureTime = DateTime.UtcNow, Duration = 60 }), nameof(LectureUpdateRequest.Name));
        AssertInvalid(new LectureUpdateRequestValidator().Validate(new LectureUpdateRequest { Name = "Name", Location = "", LectureTime = DateTime.UtcNow, Duration = 60 }), nameof(LectureUpdateRequest.Location));
        AssertInvalid(new LectureUpdateRequestValidator().Validate(new LectureUpdateRequest { Name = "Name", Location = "Room", LectureTime = default, Duration = 60 }), nameof(LectureUpdateRequest.LectureTime));
        AssertInvalid(new LectureUpdateRequestValidator().Validate(new LectureUpdateRequest { Name = "Name", Location = "Room", LectureTime = DateTime.UtcNow, Duration = 0 }), nameof(LectureUpdateRequest.Duration));
        AssertInvalid(new LectureNoteRequestValidator().Validate(new LectureNoteRequest { Title = "", Content = "Content" }), nameof(LectureNoteRequest.Title));
        AssertInvalid(new LectureNoteRequestValidator().Validate(new LectureNoteRequest { Title = "Title", Content = "" }), nameof(LectureNoteRequest.Content));
    }

    [Fact]
    public void RentalAndReferenceDataValidators_RejectMissingAndBoundaryValues()
    {
        AssertInvalid(new RentalCreateRequestValidator().Validate(new RentalCreateRequest { InstrumentId = 0 }), nameof(RentalCreateRequest.InstrumentId));
        AssertInvalid(new InstrumentCreateRequestValidator().Validate(new InstrumentCreateRequest { Model = "", Manufacturer = "Maker", InstrumentTypeId = 1 }), nameof(InstrumentCreateRequest.Model));
        AssertInvalid(new InstrumentCreateRequestValidator().Validate(new InstrumentCreateRequest { Model = "Model", Manufacturer = "", InstrumentTypeId = 1 }), nameof(InstrumentCreateRequest.Manufacturer));
        AssertInvalid(new InstrumentCreateRequestValidator().Validate(new InstrumentCreateRequest { Model = "Model", Manufacturer = "Maker", InstrumentTypeId = 0 }), nameof(InstrumentCreateRequest.InstrumentTypeId));
        AssertInvalid(new InstrumentTypeRequestValidator().Validate(new InstrumentTypeRequest { Type = "", MonthlyFee = 0 }), nameof(InstrumentTypeRequest.Type));
        AssertInvalid(new InstrumentTypeRequestValidator().Validate(new InstrumentTypeRequest { Type = new string('x', 101), MonthlyFee = 0 }), nameof(InstrumentTypeRequest.Type));
        AssertInvalid(new InstrumentTypeRequestValidator().Validate(new InstrumentTypeRequest { Type = "Guitar", MonthlyFee = -0.01m }), nameof(InstrumentTypeRequest.MonthlyFee));
        AssertInvalid(new MusicStoreRequestValidator().Validate(new MusicStoreRequest { StoreName = "", BusinessHours = "08-16" }), nameof(MusicStoreRequest.StoreName));
        AssertInvalid(new MusicStoreRequestValidator().Validate(new MusicStoreRequest { StoreName = new string('x', 101), BusinessHours = "08-16" }), nameof(MusicStoreRequest.StoreName));
        AssertInvalid(new MusicStoreRequestValidator().Validate(new MusicStoreRequest { StoreName = "Store", BusinessHours = "" }), nameof(MusicStoreRequest.BusinessHours));
        AssertInvalid(new MusicStoreRequestValidator().Validate(new MusicStoreRequest { StoreName = "Store", BusinessHours = new string('x', 51) }), nameof(MusicStoreRequest.BusinessHours));
        AssertInvalid(new AddressRequestValidator().Validate(new AddressRequest { City = "", Street = "Street", Number = "1" }), nameof(AddressRequest.City));
        AssertInvalid(new AddressRequestValidator().Validate(new AddressRequest { City = new string('x', 101), Street = "Street", Number = "1" }), nameof(AddressRequest.City));
        AssertInvalid(new AddressRequestValidator().Validate(new AddressRequest { City = "City", Street = "", Number = "1" }), nameof(AddressRequest.Street));
        AssertInvalid(new AddressRequestValidator().Validate(new AddressRequest { City = "City", Street = new string('x', 101), Number = "1" }), nameof(AddressRequest.Street));
        AssertInvalid(new AddressRequestValidator().Validate(new AddressRequest { City = "City", Street = "Street", Number = "" }), nameof(AddressRequest.Number));
        AssertInvalid(new AddressRequestValidator().Validate(new AddressRequest { City = "City", Street = "Street", Number = new string('x', 21) }), nameof(AddressRequest.Number));
    }

    [Fact]
    public void NewValidators_RejectMissingAndBoundaryValues()
    {
        AssertInvalid(new UserProvisionRequestValidator().Validate(new UserProvisionRequest { Username = "", Email = "student@example.com", Password = "Password1!", Role = "Student" }), nameof(UserProvisionRequest.Username));
        AssertInvalid(new UserProvisionRequestValidator().Validate(new UserProvisionRequest { Username = "user", Email = "bad", Password = "Password1!", Role = "Student" }), nameof(UserProvisionRequest.Email));
        AssertInvalid(new UserProvisionRequestValidator().Validate(new UserProvisionRequest { Username = "user", Email = "student@example.com", Password = "short", Role = "Student" }), nameof(UserProvisionRequest.Password));
        AssertInvalid(new UserProvisionRequestValidator().Validate(new UserProvisionRequest { Username = "user", Email = "student@example.com", Password = "Password1!", Role = "UnknownRole" }), nameof(UserProvisionRequest.Role));
        AssertInvalid(new UserProvisionRequestValidator().Validate(new UserProvisionRequest { Username = "user", Email = "student@example.com", Password = "Password1!", Role = "Student", MusicStoreId = 0 }), nameof(UserProvisionRequest.MusicStoreId));
        AssertInvalid(new UpdateMembershipRequestValidator().Validate(new UpdateMembershipRequest { PaidUntil = DateTime.UtcNow.AddDays(-1) }), nameof(UpdateMembershipRequest.PaidUntil));
        AssertInvalid(new InstrumentUpdateRequestValidator().Validate(new InstrumentUpdateRequest { Model = "" }), nameof(InstrumentUpdateRequest.Model));
        AssertInvalid(new InstrumentUpdateRequestValidator().Validate(new InstrumentUpdateRequest { Manufacturer = "" }), nameof(InstrumentUpdateRequest.Manufacturer));
        AssertInvalid(new InstrumentUpdateRequestValidator().Validate(new InstrumentUpdateRequest { Description = "" }), nameof(InstrumentUpdateRequest.Description));
        AssertInvalid(new InstrumentUpdateRequestValidator().Validate(new InstrumentUpdateRequest { ImagePath = "" }), nameof(InstrumentUpdateRequest.ImagePath));
        AssertInvalid(new InstrumentUpdateRequestValidator().Validate(new InstrumentUpdateRequest { InstrumentTypeId = 0 }), nameof(InstrumentUpdateRequest.InstrumentTypeId));
        AssertInvalid(new RsvpRequestValidator().Validate(new RsvpRequest { Note = new string('x', 501) }), nameof(RsvpRequest.Note));
        AssertInvalid(new MarkAttendanceRequestValidator().Validate(new MarkAttendanceRequest { StudentId = 0, AttendanceStatus = AttendanceStatus.Present }), nameof(MarkAttendanceRequest.StudentId));
        AssertInvalid(new MarkAttendanceRequestValidator().Validate(new MarkAttendanceRequest { StudentId = 1, AttendanceStatus = (AttendanceStatus)99 }), nameof(MarkAttendanceRequest.AttendanceStatus));
    }

    [Fact]
    public void AnnouncementValidator_RejectsMissingRequiredFields()
    {
        AssertInvalid(new AnnouncementRequestValidator().Validate(new AnnouncementRequest("", "Content")), nameof(AnnouncementRequest.Title));
        AssertInvalid(new AnnouncementRequestValidator().Validate(new AnnouncementRequest("Title", "")), nameof(AnnouncementRequest.Content));
    }

    private static LectureCreateRequest ValidLectureCreate() => new() { CourseId = 1, Name = "Name", Location = "Room", LectureTime = DateTime.UtcNow, Duration = 60 };
    private static LectureUpdateRequest ValidLectureUpdate() => new() { Name = "Name", Location = "Room", LectureTime = DateTime.UtcNow, Duration = 60 };
    private static UserProvisionRequest ValidUserProvision() => new()
    {
        Username = "user",
        Email = "student@example.com",
        Password = "Password1!",
        Role = "Student",
        MusicStoreId = 1
    };
    private static void AssertInvalid(ValidationResult result, string propertyName) => Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
}
