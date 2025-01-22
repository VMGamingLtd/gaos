#pragma warning disable 8632
namespace Gaos.Routes.Model.FriendJson
{
    public class UserForFriendsSearch 
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public bool IsMyFriend { get; set; }
        public bool IsMyFriendRequest { get; set; }
        public bool IsFriendRequestToMe { get; set; }
    };

    [System.Serializable]
    public class GetUsersForFriendsSearchResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }
        public UserForFriendsSearch[] Users { get; set; }
    }
}
