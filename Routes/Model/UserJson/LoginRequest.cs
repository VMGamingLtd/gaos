#pragma warning disable 8632
using System.Security.Cryptography;

namespace Gaos.Routes.Model.UserJson
{

    [System.Serializable]
    public class LoginRequest
    {
        public string? UserName { get; set; }

        public string? Password { get; set; }

        public int? DeviceId { get; set; }
    }
}
