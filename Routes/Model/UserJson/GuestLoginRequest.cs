#pragma warning disable 8632
namespace Gaos.Routes.Model.UserJson
{

    [System.Serializable]
    public class GuestLoginRequest
    {
        public string? UserName { get; set; }
        public int? DeviceId { get; set; }
    }
}