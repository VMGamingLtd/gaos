#pragma warning disable 8632
namespace Gaos.Routes.Model.ChatRoomJson
{
    [System.Serializable]
    public class GetUserToFriendChatRoomResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public int? ChatRoomId { get; set; }
        public string? ChatRoomName { get; set; }
    }
}
