#pragma warning disable 8632

namespace Gaos.Routes.Model.UserJson
{
    public enum RegisterResponseErrorKind
    {
        UsernameExistsError,
        UserNameIsEmptyError,
        EmailIsEmptyError,
        IncorrectEmailError,
        EmailExistsError,
        PasswordIsEmptyError,
        PasswordsDoNotMatchError,
        InternalError,
    };

    [System.Serializable]
    public class RegisterResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public RegisterResponseErrorKind? ErrorKind { get; set; }

        public Dbo.Model.User? User { get; set; }

        public string? Jwt { get; set; }
    }
}
