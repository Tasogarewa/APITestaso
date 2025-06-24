namespace Backend.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public DateTime Expires { get; set; }
        public bool IsRevoked { get; set; }
        public bool IsExpired => DateTime.UtcNow >= Expires;
    }
}
