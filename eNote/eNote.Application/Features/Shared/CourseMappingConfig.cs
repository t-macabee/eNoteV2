using eNote.Application.Features.Academic.Courses;
using Mapster;

namespace eNote.Application.Features.Shared;

public sealed class CourseMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Course, CourseDto>()
            .Map(dest => dest.EnrolledCount, src => src.Enrollments == null ? 0 : src.Enrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active));
    }
}