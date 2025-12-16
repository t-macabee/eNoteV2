using eNote.Domain.Entities.Base;

namespace eNote.Domain.Entities
{
    public class MusicShop : BaseEntity
    {        
        public string StoreName { get; set; } = null!;
        public string BusinessHours { get; set; } = null!;

        public int AppUserId { get; set; }
    }
}
