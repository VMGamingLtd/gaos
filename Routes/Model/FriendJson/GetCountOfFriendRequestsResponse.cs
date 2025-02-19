#pragma warning disable 8632
namespace Gaos.Routes.Model.FriendJson
{
    [System.Serializable]
    public class GetCountOfFriendRequestsResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }
        public int Count { get; set; }
    }
}
