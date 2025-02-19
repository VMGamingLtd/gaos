#pragma warning disable 8632
namespace Gaos.Dbo.Model
{
    [System.Serializable]
    public class ChatRoomUser
    {
        public int Id { get; set; }
        public int ChatRoomId { get; set; }
        public ChatRoom? ChatRoom { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public int LastReadMessageId { get; set; }
    }
}
