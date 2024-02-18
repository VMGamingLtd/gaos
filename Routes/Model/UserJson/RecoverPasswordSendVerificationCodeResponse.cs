#pragma warning disable 8632
namespace Gaos.Routes.Model.UserJson
{
    public enum RecoverPasswordSendVerificationCodeErrorKind
    {
        UserNameOrEmailNotFound,
        InternalError,
    };

    public class RecoverPasswordSendVerificationCodeResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }
        public RecoverPasswordSendVerificationCodeErrorKind? ErrorKind { get; set; }

        public int? UserId { get; set; }
    }
}
