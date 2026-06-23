using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Mapster;

namespace eNote.Application.Features.Courses;

public sealed class CourseMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Course, CourseDto>()
            .Map(dest => dest.EnrolledCount, src => src.Enrollments == null ? 0 : src.Enrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active));
    }
}
