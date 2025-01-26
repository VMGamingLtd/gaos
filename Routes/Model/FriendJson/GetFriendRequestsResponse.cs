#pragma warning disable 8632
namespace Gaos.Routes.Model.FriendJson
{
    public class FriendRequest 
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
    };

    [System.Serializable]
    public class GetFriendRequestsResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }
        public FriendRequest[] FriendRequest { get; set; }
    }
}
