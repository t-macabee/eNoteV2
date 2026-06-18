namespace eNote.Application.Features.Users
{
    public class UserProvisionRequest
    {
        public required string Username { get; init; }
        public required string Email { get; init; }
        public required string Password { get; init; }
        public required string Role { get; init; }

        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public int? MusicStoreId { get; init; }
    }
}
