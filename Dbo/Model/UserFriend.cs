namespace Gaos.Dbo.Model
{
    [System.Serializable]
    public class UserFriend
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public User? User { get; set; }
        public int? FriendUserId { get; set; }
        public User? FriendUser { get; set; }
    }
}
