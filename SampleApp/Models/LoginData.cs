namespace SampleApp.Models
{

    public class LoginData
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public Guid SessionId { get; set; }
    }
}