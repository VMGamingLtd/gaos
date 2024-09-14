#pragma warning disable 8632

using Gaos.Dbo.Model;

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
        public UserInterfaceColors? UserInterfaceColors { get; set; }

        public string? Jwt { get; set; }
    }
}
