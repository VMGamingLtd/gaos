#pragma warning disable 8632
namespace Gaos.Dbo.Model
{
    [System.Serializable]
    public class ChatRoomMessage
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string? ChatRoomMemberName { get; set; }
        public int ChatRoomId { get; set; }
        public ChatRoom? ChatRoom { get; set; }
        public int MessageId { get; set; }
        public string? Message { get; set; }
        public System.DateTime CreatedAt { get; set; }

    }
}
