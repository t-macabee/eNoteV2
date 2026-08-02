using eNote.Application.Features.Academic.Lectures;
using Mapster;

namespace eNote.Application.Features.Mapping;

public sealed class LectureMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Lecture, LectureDto>()
            .Map(dest => dest.AttendeeCount, src => src.Attendances.Count(a => a.AttendanceStatus == AttendanceStatus.Present));
    }
}
