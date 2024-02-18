#pragma warning disable 8632
namespace Gaos.Dbo.Model
{
    public class UserEmail
    {
        public int Id { get; set; }
        public string? Email { get; set; }
        public int? UserId { get; set; }
        public User? User { get; set; }
        public string? EmailVerificationCode { get; set; }
        public bool? IsEmailVerified { get; set; }
    }
}
