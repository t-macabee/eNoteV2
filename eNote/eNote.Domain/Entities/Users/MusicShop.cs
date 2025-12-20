using eNote.Domain.Entities.Base;

namespace eNote.Domain.Entities.Users
{
    public class MusicShop : BaseEntity
    {        
        public int UserId { get; set; }
        public string StoreName { get; set; } = null!;
        public string BusinessHours { get; set; } = null!;

        protected MusicShop() { }

        public MusicShop(int userId, string storeName, string businessHours)
        {
            UserId = userId;
            StoreName = storeName;
            BusinessHours = businessHours;
        }
    }
}
