#pragma warning disable 8632
namespace Gaos.Routes.Model.UserJson
{
    public class RecoverPasswordVerifyCodeRequest
    {
        public int UserId { get; set; }
        public string? VerificationCode { get; set; }
    }
}
