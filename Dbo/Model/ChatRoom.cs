#pragma warning disable 8632
namespace Gaos.Dbo.Model
{
    [System.Serializable]
    public class ChatRoom
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int OwnerId { get; set; }
        public User? Owner { get; set; }
        public bool IsFriedndsChatroom { get; set; }
        public bool IsGroupChatroom { get; set; }

    }
}
