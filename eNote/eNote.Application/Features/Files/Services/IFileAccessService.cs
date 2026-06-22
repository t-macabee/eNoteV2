namespace eNote.Application.Features.Files.Services;

public interface IFileAccessService
{
    Task<bool> CanAccessAssignmentFileAsync(int userId, string fileName, CancellationToken cancellationToken = default);
}