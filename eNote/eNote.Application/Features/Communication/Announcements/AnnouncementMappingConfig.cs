using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Mapster;

namespace eNote.Application.Features.Communication.Announcements;

public sealed class AnnouncementMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Announcement, AnnouncementDto>()
            .Map(dest => dest.Scope, src => src.CourseId.HasValue ? AnnouncementScope.Course : AnnouncementScope.MusicStore)
            .Map(dest => dest.CourseName, src => src.Course == null ? null : src.Course.Name)
            .Map(dest => dest.StoreName, src => src.MusicStore == null ? null : src.MusicStore.StoreName);
    }
}
