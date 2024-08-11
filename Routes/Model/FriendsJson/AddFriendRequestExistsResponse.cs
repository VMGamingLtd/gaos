#pragma warning disable 8632
namespace Gaos.Routes.Model.FriendsJson
{
    [System.Serializable]
    public class AddFriendRequestExistsResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public bool? Exists { get; set; }
    }
}
