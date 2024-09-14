#pragma warning disable 8632
using Gaos.Dbo.Model;

namespace Gaos.Routes.Model.UserJson
{
    [System.Serializable]
    public class GuestLoginResponse
    {
        public bool? IsError { get; set; }

        public string? ErrorMessage { get; set; }
        public string? UserName { get; set; }
        public string? Country { get; set; }
        public string? Language { get; set; }
        public UserInterfaceColors? UserInterfaceColors { get; set; }
        public int UserId { get; set; }

        public bool? IsGuest { get; set; }

        public string? Jwt { get; set; }

    }
}