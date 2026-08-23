using eNote.Application.Common.Interfaces;

namespace eNote.Worker;

internal sealed class WorkerActor : ICurrentUserContext
{
    public int UserId => throw new NotSupportedException();
    public bool IsAuthenticated => false;
}
