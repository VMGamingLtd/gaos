#pragma warning disable 8632
namespace Gaos.Routes.Model.GameDataJson
{
    [System.Serializable]
    public class UserGameDataSaveResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public string? Id { get; set; }
        public string? Version { get; set; }

        public string? GameDataJson { get; set; }

    }
}
