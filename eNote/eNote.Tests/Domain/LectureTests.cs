namespace eNote.Tests.Domain;

public sealed class LectureTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void MarkHeld_FromScheduled_SetsHeld()
    {
        var lecture = new Lecture("L", "Room 1", 60, DateTime.UtcNow, LectureType.Theoretical, null, 1);

        lecture.MarkHeld();

        Assert.Equal(LectureStatus.Held, lecture.LectureStatus);
    }

    [Fact]
    public void MarkHeld_WhenAlreadyHeld_DoesNothing()
    {
        var lecture = new Lecture("L", "Room 1", 60, DateTime.UtcNow, LectureType.Theoretical, null, 1);
        lecture.MarkHeld();

        lecture.MarkHeld();

        Assert.Equal(LectureStatus.Held, lecture.LectureStatus);
    }

    [Fact]
    public void MarkHeld_WhenCancelled_Throws()
    {
        var lecture = new Lecture("L", "Room 1", 60, DateTime.UtcNow, LectureType.Theoretical, null, 1);
        lecture.Cancel("Reason", 5, Now);

        Assert.Throws<InvalidOperationException>(() => lecture.MarkHeld());
    }

    [Fact]
    public void Cancel_FromScheduled_StoresReasonAndStatus()
    {
        var lecture = new Lecture("L", "Room 1", 60, DateTime.UtcNow, LectureType.Theoretical, null, 1);

        lecture.Cancel("  Instrument je pokvaren  ", 7, Now);

        Assert.Equal(LectureStatus.Cancelled, lecture.LectureStatus);
        Assert.True(lecture.IsCancelled);
        Assert.Equal("Instrument je pokvaren", lecture.CancellationReason);
        Assert.Equal(Now, lecture.CancelledAt);
        Assert.Equal(7, lecture.CancelledById);
    }

    [Fact]
    public void Cancel_WhenHeld_Throws()
    {
        var lecture = new Lecture("L", "Room 1", 60, DateTime.UtcNow, LectureType.Theoretical, null, 1);
        lecture.MarkHeld();

        Assert.Throws<InvalidOperationException>(() => lecture.Cancel("Reason", 5, Now));
        Assert.Equal(LectureStatus.Held, lecture.LectureStatus);
        Assert.Null(lecture.CancellationReason);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_Throws()
    {
        var lecture = new Lecture("L", "Room 1", 60, DateTime.UtcNow, LectureType.Theoretical, null, 1);
        lecture.Cancel("First reason", 5, Now);

        Assert.Throws<InvalidOperationException>(() => lecture.Cancel("Second reason", 5, Now));
        Assert.Equal("First reason", lecture.CancellationReason);
    }
}
