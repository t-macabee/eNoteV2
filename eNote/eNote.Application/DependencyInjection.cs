using eNote.Application.Features.Academic.Assignments.Services;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Academic.LectureNotes.Services;
using eNote.Application.Features.Academic.Lectures.Services;
using eNote.Application.Features.Communication.Announcements.Services;
using eNote.Application.Features.Communication.Notifications.Services;
using eNote.Application.Features.Files.Services;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Application.Features.Rentals.Instruments.Services;
using eNote.Application.Features.Rentals.Recommendations.Services;
using eNote.Application.Features.Rentals.ReferenceData.Addresses;
using eNote.Application.Features.Rentals.ReferenceData.InstrumentTypes;
using eNote.Application.Features.Rentals.ReferenceData.MusicStores;
using Microsoft.Extensions.DependencyInjection;

namespace eNote.Application;

public static class DependencyInjection
{

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAddressService, AddressService>();
        services.AddScoped<IFileAccessService, FileAccessService>();
        services.AddScoped<IInstrumentTypeService, InstrumentTypeService>();
        services.AddScoped<IMusicStoreService, MusicStoreService>();
        services.AddScoped<IStudentDisplayNameService, StudentDisplayNameService>();
        services.AddScoped<IUserProvisioningService, UserProvisioningService>();

        services.AddScoped<AssignmentService>();
        services.AddScoped<AssignmentSubmissionService>();
        services.AddScoped<AdminInstructorService>();
        services.AddScoped<InstructorAnnouncementService>();
        services.AddScoped<StoreAnnouncementService>();
        services.AddScoped<StudentAnnouncementFeedService>();
        services.AddScoped<CourseService>();
        services.AddScoped<CourseEnrollmentService>();
        services.AddScoped<InstrumentService>();
        services.AddScoped<InstructorAccessService>();
        services.AddScoped<LectureAttendanceService>();
        services.AddScoped<LectureNoteService>();
        services.AddScoped<LectureService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<RankingService>();
        services.AddScoped<RentalCommandService>();
        services.AddScoped<RentalQueryService>();
        services.AddScoped<RecommendationService>();
        services.AddScoped<UserProfileService>();
        services.AddScoped<UserSelfService>();

        services.AddScoped<IUserProfileLookup, UserProfileLookup>();
        services.AddScoped<ICurrentActor, CurrentActor>();

        return services;
    }
}
