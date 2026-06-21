using eNote.Domain.Entities.Base;

namespace eNote.Domain.Entities
{
    public class MusicStore : AuditableEntity
    {
        public string StoreName { get; private set; } = null!;
        public string BusinessHours { get; private set; } = null!;

        public ICollection<MusicStoreEmployee> Employees { get; private set; } = new List<MusicStoreEmployee>();
        public ICollection<Instrument> Instruments { get; private set; } = new List<Instrument>();

        protected MusicStore()
        {
        }

        public MusicStore(string storeName, string businessHours)
        {
            StoreName = storeName;
            BusinessHours = businessHours;
        }

        public void UpdateDetails(string storeName, string businessHours)
        {
            StoreName = storeName;
            BusinessHours = businessHours;
        }
    }
}
