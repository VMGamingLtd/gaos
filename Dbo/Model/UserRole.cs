#pragma warning disable CS8632

namespace Gaos.Dbo.Model
{
    [System.Serializable]
    public class UserRole
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public User? User { get; set; }
        public int? RoleId { get; set; }
        public Role? Role { get; set; }
    }
}
