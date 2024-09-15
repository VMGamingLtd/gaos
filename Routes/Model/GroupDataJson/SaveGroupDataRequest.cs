#pragma warning disable 8632
namespace Gaos.Routes.Model.GroupDataJson
{
    [System.Serializable]
    public class SaveGroupDataRequest
    {
        public int SlotId { get; set; }
        public string? GroupDataJson { get; set; }
        public int Version { get; set; }
    }
}
