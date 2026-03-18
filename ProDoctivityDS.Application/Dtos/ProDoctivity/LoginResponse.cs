namespace ProDoctivityDS.Application.Dtos.ProDoctivity
{
    public class LoginResponse
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string OrganizationId { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string PositionRole { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }
}
