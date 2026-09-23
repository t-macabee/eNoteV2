namespace eNote.Tests.Domain;

public sealed class LectureTests
{
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
        lecture.Cancel();

        Assert.Throws<InvalidOperationException>(() => lecture.MarkHeld());
    }
}
