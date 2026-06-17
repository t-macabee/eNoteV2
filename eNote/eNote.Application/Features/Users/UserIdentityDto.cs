namespace eNote.Application.Features.Users
{
    public class UserIdentityDto
    {
        public int Id
        {
            get; set;
        }
        public string Username { get; set; } = null!;
        public string? FirstName
        {
            get; set;
        }
        public string? LastName
        {
            get; set;
        }
        public DateTime? DateOfBirth
        {
            get; set;
        }
        public AddressDto? Address
        {
            get; set;
        }
        public bool IsActive
        {
            get; set;
        }
    }
}
