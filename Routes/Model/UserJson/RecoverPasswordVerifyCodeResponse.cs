#pragma warning disable 8632
namespace Gaos.Routes.Model.UserJson
{
    public enum RecoverPasswordVerifyCodeReplyErrorKind
    {
        InternalError,
    };

    public class RecoverPasswordVerifyCodeResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }
        public RecoverPasswordVerifyCodeReplyErrorKind? ErrorKind { get; set; }

        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public bool? IsVerified { get; set; }
    }
}
