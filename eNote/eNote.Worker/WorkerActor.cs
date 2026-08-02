using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Domain.Entities.Academic;
using eNote.Domain.Entities.Identity;

namespace eNote.Worker;

internal sealed class WorkerActor : ICurrentActor
{
    public int UserId => throw new NotSupportedException();
    public bool IsAuthenticated => false;

    public Task<Student> GetCurrentStudentAsync() => throw new NotSupportedException();
    public Task<int> GetCurrentStudentIdAsync() => throw new NotSupportedException();
    public Task<Instructor> GetCurrentInstructorAsync() => throw new NotSupportedException();
    public Task<MusicStoreEmployee> GetCurrentEmployeeAsync() => throw new NotSupportedException();
    public Task<int> GetCurrentStoreIdAsync(CancellationToken cancellationToken = default) => throw new StoreNotResolvedException();
    public int GetCurrentStoreId() => throw new StoreNotResolvedException();
}
