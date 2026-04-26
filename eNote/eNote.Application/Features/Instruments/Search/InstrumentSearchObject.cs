using eNote.Application.Common.Search;

namespace eNote.Application.Features.Instruments.Search
{
    public class InstrumentSearchObject : BaseSearchObject
    {
        public string? Model { get; set; }
        public string? Manufacturer { get; set; }
        public bool? IsAvailable { get; set; }
        public int? InstrumentTypeId { get; set; }
        public int? MusicStoreId { get; set; }
    }
}
