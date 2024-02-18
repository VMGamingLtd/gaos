#pragma warning disable 8632
namespace Gaos.Routes.Model.GameDataJson
{
    [System.Serializable]
    public class UserGameDataGetRequest
    {
        public int UserId { get; set; }
        public int SlotId { get; set; }

        public string? Version { get; set; }
    }
}
