#pragma warning disable 8632
namespace Gaos.Routes.Model.ChatRoomJson
{
    [System.Serializable]
    public class GetUnreadMessagesCountResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public int userId  { get; set; }
        public int count { get; set; }
    }
}
