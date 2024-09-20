#pragma warning disable 8632
namespace Gaos.Routes.Model.GroupDataJson
{
    [System.Serializable]
    public class GetGroupDataGetRequest
    {
        public int SlotId { get; set; }
        public  long Version { get; set; }

        public bool IsGameDataDiff { get; set; }
    }
}
