using System.Text.Json.Serialization;

namespace API.DTOs
{
    public class LogInDto
    {
        [JsonPropertyName("username")]
        public string UserName { get; set; } = "";

        [JsonPropertyName("password")]
        public string Password { get; set; } = "";
    }
}