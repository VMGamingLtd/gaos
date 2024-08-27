#pragma warning disable 8632

namespace Gaos.Dbo.Model
{
    [System.Serializable]
    public class User
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool? IsGuest { get; set; }
        public string? Email { get; set; }
        public string? PasswordHash { get; set; }
        public string? PasswordSalt { get; set; }
        public int? DeviceId { get; set; }
        public Device? Device { get; set; }
        public string? EmailVerificationCode { get; set; }
        public bool? IsEmailVerified { get; set; }
        public string? Country { get; set; }

    }
}
