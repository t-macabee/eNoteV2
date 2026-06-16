using eNote.Application.Features.Courses;
using eNote.Application.Features.InstrumentRentals;
using eNote.Application.Features.Instruments;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Mapster;

namespace eNote.Application.Mapping
{
    public sealed class MapsterConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Course, CourseDto>()
                .Map(dest => dest.EnrolledCount, src => src.Enrollments == null 
                ? 0 : src.Enrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active));

            config.NewConfig<Instrument, InstrumentDto>()
                .Map(dest => dest.InstrumentType, src => src.InstrumentType.Type)
                .Map(dest => dest.MusicStore, src => src.MusicStore.StoreName);

            TypeAdapterConfig<InstrumentRental, InstrumentRentalDto>.NewConfig()
                .Map(x => x.InstrumentModel, x => x.Instrument.Model)
                .Map(x => x.InstrumentType, x => x.Instrument.InstrumentType.Type)
                .Map(x => x.MusicStoreId, x => x.Instrument.MusicStoreId)
                .Map(x => x.StoreName, x => x.Instrument.MusicStore.StoreName)
                .Map(x => x.StudentProfileId, x => x.StudentProfileId)
                .Map(x => x.Fee, x => x.Fee);

            config.Compile();
        }
    }
}
