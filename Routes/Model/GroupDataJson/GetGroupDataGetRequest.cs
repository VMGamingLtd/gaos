#pragma warning disable 8632
namespace Gaos.Routes.Model.GroupDataJson
{
    [System.Serializable]
    public class GetGroupDataGetRequest
    {
        public int UserId { get; set; }
        public int SlotId { get; set; }

        public string? Version { get; set; }
    }
}
