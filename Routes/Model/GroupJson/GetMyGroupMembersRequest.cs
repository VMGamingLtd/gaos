namespace Gaos.Routes.Model.GroupJson
{

    [System.Serializable]
    public class GetMyGroupMembersRequest
    {
        public int GroupId { get; set; }
        public  int MaxCount { get; set; }
        public bool IsCountOnly { get; set; }
    }
}
