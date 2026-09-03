namespace eNote.Domain.Entities.Identity;

public class MusicStoreEmployee : AuditableEntity, ITenantScoped
{
    public int AppUserId { get; private set; }
    public int MusicStoreId { get; private set; }
    public MusicStore MusicStore { get; private set; } = null!;

    public bool IsManager { get; private set; }
    public bool IsActive { get; set; } = true;

    protected MusicStoreEmployee()
    {
    }

    public MusicStoreEmployee(int appUserId, int musicStoreId, bool isManager)
    {
        AppUserId = appUserId;
        MusicStoreId = musicStoreId;
        IsManager = isManager;
    }

    public void SetManager(bool isManager) => IsManager = isManager;
}
