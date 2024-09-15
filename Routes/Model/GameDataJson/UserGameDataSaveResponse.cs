#pragma warning disable 8632
namespace Gaos.Routes.Model.GameDataJson
{
    public enum UserGameDataSaveErrorKind
    {
        JsonDiffBaseMismatchError,
        VersionMismatchError,
        InternalError,
    }

    [System.Serializable]
    public class UserGameDataSaveResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }
        public UserGameDataSaveErrorKind? ErrorKind { get; set; }

        public string? Id { get; set; }
        public int Version { get; set; }

        public string? GameDataJson { get; set; }

    }
}
