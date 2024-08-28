#pragma warning disable 8632
namespace Gaos.Routes.Model.FriendsJson
{
    [System.Serializable]
    public class GetFriendRequestsRequest
    {
        public string? OwnerNamePattern { get; set; }
        public int MaxCount { get; set; }
    }
}
