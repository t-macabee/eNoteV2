using eNote.Domain.Entities.Base;

namespace eNote.Domain.Entities.Users
{
    public class MusicShop : BaseEntity
    {        
        public int AppUserId { get; set; }
        public string StoreName { get; set; } = null!;
        public string BusinessHours { get; set; } = null!;

        protected MusicShop() { }   

        public MusicShop(int appUserId, string storeName, string businessHours)
        {
            AppUserId = appUserId;
            StoreName = storeName;
            BusinessHours = businessHours;
        }
    }
}
