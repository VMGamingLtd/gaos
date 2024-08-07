namespace Gaos.Routes.Model.FriendsJson
{

    [System.Serializable]
    public class GetGroupMembersRequest
    {
        public int GroupId { get; set; }
        public  int MaxCount { get; set; }
    }
}
