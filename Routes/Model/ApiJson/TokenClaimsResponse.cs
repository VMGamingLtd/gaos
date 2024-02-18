#pragma warning disable 8632

namespace Gaos.Routes.Model.ApiJson
{
    [System.Serializable]
    public class TokenClaimsResponse
    {
        public bool? IsError { get; set; }

        public string? ErrorMessage { get; set; }

        public Gaos.Model.Token.TokenClaims? TokenClaims { get; set; }


    }
}
