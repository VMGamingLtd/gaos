#pragma warning disable 8632
namespace Gaos.Routes.Model.GroupJson
{
    [System.Serializable]
    public class RejectFriendRequestResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
