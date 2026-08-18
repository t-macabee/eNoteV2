using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Domain.Entities.Identity;

namespace eNote.Worker;

internal sealed class WorkerActor : ICurrentActor
{
    public int UserId => throw new NotSupportedException();
    public bool IsAuthenticated => false;

    public Task<Student> GetCurrentStudentAsync() => Task.FromException<Student>(new NotSupportedException());
    public Task<int> GetCurrentStudentIdAsync() => Task.FromException<int>(new NotSupportedException());
    public Task<Instructor> GetCurrentInstructorAsync() => Task.FromException<Instructor>(new NotSupportedException());
    public Task<MusicStoreEmployee> GetCurrentEmployeeAsync() => Task.FromException<MusicStoreEmployee>(new NotSupportedException());
    public Task<int> GetCurrentStoreIdAsync(CancellationToken cancellationToken = default) => Task.FromException<int>(new StoreNotResolvedException());
    public int GetCurrentStoreId() => throw new StoreNotResolvedException();
}
