#pragma warning disable 8632
namespace gaos.Routes.Model.FriendJson
{
    public class UserForFriendsSearch 
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public bool IsFriend { get; set; }
        public bool IsFriendRequest { get; set; }
    };

    [System.Serializable]
    public class GetUsersForFriendsSearchResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }
        public UserForFriendsSearch[] Users { get; set; }
    }
}
