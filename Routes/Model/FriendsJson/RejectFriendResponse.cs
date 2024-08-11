#pragma warning disable 8632
namespace Gaos.Routes.Model.FriendsJson
{
    [System.Serializable]
    public class RejectFriendResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
