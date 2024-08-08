namespace Gaos.Routes.Model.FriendsJson
{
    public class GetFriendRequestsResponseListItem
    {
        public int GroupId { get; set; }
        public string? GroupName { get; set; }
        public int GroupOwnerId { get; set; }
        public string? GroupOwnerName { get; set; }
    }
    [System.Serializable]
    public class GetFriendRequestsResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public List<GetFriendRequestsResponseListItem>? FriendRequests { get; set; }
    }
}
