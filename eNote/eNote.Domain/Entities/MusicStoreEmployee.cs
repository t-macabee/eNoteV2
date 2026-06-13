using eNote.Domain.Entities.Base;

namespace eNote.Domain.Entities
{
    public class MusicStoreEmployee : AuditableEntity
    {
        public int AppUserId { get; set; }
        public int MusicStoreId { get; set; }

        public bool IsManager { get; set; }
        public bool IsActive { get; set; } = true;

        protected MusicStoreEmployee() { }

        public MusicStoreEmployee(int appUserId, int musicStoreId, bool isManager)
        {
            AppUserId = appUserId;
            MusicStoreId = musicStoreId;
            IsManager = isManager;
        }

        public MusicStore MusicStore { get; set; } = null!;
    }
}
