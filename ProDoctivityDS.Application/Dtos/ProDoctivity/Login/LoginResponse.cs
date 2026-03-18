namespace ProDoctivityDS.Application.Dtos.ProDoctivity.Login
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
    }

}
