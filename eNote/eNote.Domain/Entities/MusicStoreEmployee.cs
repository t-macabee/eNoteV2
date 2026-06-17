using eNote.Domain.Entities.Base;

namespace eNote.Domain.Entities
{
    public class MusicStoreEmployee : AuditableEntity
    {
        public int AppUserId
        {
            get; private set;
        }
        public int MusicStoreId
        {
            get; private set;
        }

        public bool IsManager
        {
            get; private set;
        }
        public bool IsActive { get; set; } = true;

        public MusicStore MusicStore { get; private set; } = null!;

        protected MusicStoreEmployee()
        {
        }

        public MusicStoreEmployee(int appUserId, int musicStoreId, bool isManager)
        {
            AppUserId = appUserId;
            MusicStoreId = musicStoreId;
            IsManager = isManager;
        }
    }
}
