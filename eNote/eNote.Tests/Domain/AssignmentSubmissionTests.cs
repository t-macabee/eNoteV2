using eNote.Domain.Entities.Assignments;

namespace eNote.Tests.Domain;

public sealed class AssignmentSubmissionTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void SetGrade_OutOfRange_ThrowsArgumentOutOfRangeException(int grade)
    {
        var submission = new AssignmentSubmission(1, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => submission.SetGrade(grade));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(55)]
    [InlineData(100)]
    public void SetGrade_WithinRange_SetsGrade(int grade)
    {
        var submission = new AssignmentSubmission(1, 1);

        submission.SetGrade(grade);

        Assert.Equal(grade, submission.Grade);
    }

    [Fact]
    public void SetGrade_StoresTrimmedFeedback()
    {
        var submission = new AssignmentSubmission(1, 1);

        submission.SetGrade(85, "  Dobro  ");

        Assert.Equal("Dobro", submission.Feedback);
    }

    [Fact]
    public void SetGrade_BlankFeedback_ClearsIt()
    {
        var submission = new AssignmentSubmission(1, 1);
        submission.SetGrade(85, "Dobro");

        submission.SetGrade(80, "   ");

        Assert.Null(submission.Feedback);
    }
}
