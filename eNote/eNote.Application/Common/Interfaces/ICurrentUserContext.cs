namespace eNote.Application.Common.Interfaces;

public interface ICurrentUserContext
{
    int UserId { get; }
    bool IsAuthenticated { get; }
}
