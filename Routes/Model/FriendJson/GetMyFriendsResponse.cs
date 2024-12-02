namespace Gaos.Routes.Model.FriendJson
{
    public class UserForGetMyFriends 
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
    };

    [System.Serializable]
    public class GetMyFriendsResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }
        public UserForGetMyFriends[] Users { get; set; }
    }
}
