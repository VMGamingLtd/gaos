#pragma warning disable 8632

namespace Gaos.Routes.Model.UserJson
{
    [System.Serializable]
    public class VerifyEmailRequest
    {
        public string? Code { get; set; }
    }
}
