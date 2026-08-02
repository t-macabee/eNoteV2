using eNote.Application.Features.Academic.Courses;
using Mapster;

namespace eNote.Application.Features.Mapping;

public sealed class CourseMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Course, CourseDto>()
            .Map(dest => dest.EnrolledCount, src => src.Enrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active));
    }
}
