using eNote.Application.Features.Academic.Assignments.Services;
using eNote.Application.Features.Academic.Courses.Services;
using eNote.Application.Features.Academic.LectureNotes.Services;
using eNote.Application.Features.Academic.Lectures.Services;
using eNote.Application.Features.Communication.Announcements.Services;
using eNote.Application.Features.Communication.Notifications.Services;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Rentals.InstrumentRentals.Services;
using eNote.Application.Features.Rentals.InstrumentRentals.StateMachine;
using eNote.Application.Features.Rentals.Instruments.Services;
using eNote.Application.Features.Rentals.Recommendations.Services;
using Microsoft.Extensions.DependencyInjection;

namespace eNote.Application;

public static class DependencyInjection
{

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<CourseService>()
            .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Service") && !type.IsAbstract))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.AddScoped<AssignmentService>();
        services.AddScoped<AssignmentSubmissionService>();
        services.AddScoped<AdminInstructorService>();
        services.AddScoped<AnnouncementService>();
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

        services.AddScoped<IRentalStateMachine, RentalStateMachine>();
        services.AddScoped<IUserProfileLookup, UserProfileLookup>();
        services.AddScoped<ICurrentActor, CurrentActor>();

        return services;
    }
}
