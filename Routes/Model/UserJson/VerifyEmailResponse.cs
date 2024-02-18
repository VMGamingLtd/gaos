#pragma warning disable 8632
namespace Gaos.Routes.Model.UserJson
{
    public enum VerifyEmailResponseErrorKind
    {
        InvalidCodeError,
        InternalError,
    };

    public class VerifyEmailResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public string? Email { get; set; }

        public VerifyEmailResponseErrorKind? ErrorKind { get; set; }
    }
}
