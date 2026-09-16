namespace eNote.Application.Features.Identity.Users;

public class UserProvisionRequest : DelegatedUserCreateRequest
{
    public required string Role { get; init; }

    public int? MusicStoreId { get; init; }
    public bool? IsManager { get; init; }
}
