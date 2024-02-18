#pragma warning disable 8618
using System;

namespace Gaos.Model.Token
{ 
    public enum UserType
    {
        RegisteredUser,
        GuestUser,
    }

    [System.Serializable]
    public class TokenClaims
    {
        public string Sub { get; set; }
        public long Exp { get; set; }

        public int UserId { get; set; }

        public UserType UserType { get; set; }

        public int DeviceId { get; set; }
    }
}
