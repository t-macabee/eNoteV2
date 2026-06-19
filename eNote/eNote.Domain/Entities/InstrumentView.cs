namespace eNote.Domain.Entities;

public class InstrumentView
{
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int InstrumentId { get; private set; }

    public int ViewCount { get; private set; }
    public DateTime LastViewedAt { get; private set; }

    protected InstrumentView()
    {
    }

    public InstrumentView(int userId, int instrumentId, DateTime viewedAt)
    {
        UserId = userId;
        InstrumentId = instrumentId;
        ViewCount = 1;
        LastViewedAt = viewedAt;
    }

    public void RecordView(DateTime viewedAt)
    {
        ViewCount++;
        LastViewedAt = viewedAt;
    }
}
