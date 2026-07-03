using Mapster;

namespace eNote.Application.Features.Academic.Lectures;

public sealed class LectureMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Lecture, LectureDto>()
            .Map(dest => dest.AttendeeCount, src => src.Attendances == null ? 0 : src.Attendances.Count(a => a.AttendanceStatus == AttendanceStatus.Present));
    }
}