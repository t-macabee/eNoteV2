using eNote.Domain.Entities.Base;

namespace eNote.Domain.Entities
{
    public class MusicStore : AuditableEntity
    {        
        public string StoreName { get; set; } = null!;
        public string BusinessHours { get; set; } = null!;

        protected MusicStore() { }   

        public MusicStore(string storeName, string businessHours)
        {            
            StoreName = storeName;
            BusinessHours = businessHours;
        }

        public ICollection<MusicStoreEmployee> Employees { get; set; } = new List<MusicStoreEmployee>();
        public ICollection<Instrument> Instruments { get; set; } = new List<Instrument>();
    }
}
