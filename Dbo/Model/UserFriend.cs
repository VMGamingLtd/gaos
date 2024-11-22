#pragma warning disable 8632
namespace Gaos.Dbo.Model
{
    [System.Serializable]
    public class UserFriend
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public User? User { get; set; }
        public int? FriendId { get; set; }
        public User? Friend { get; set; }
        public bool? IsFriendAgreement{ get; set; }
    }
}
