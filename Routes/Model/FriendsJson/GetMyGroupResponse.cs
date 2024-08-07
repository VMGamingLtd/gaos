namespace Gaos.Routes.Model.FriendsJson
{
    [System.Serializable]
    public class GetMyGroupResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public bool IsGroupOwner { get; set; }
        public bool IsGroupMember { get; set; }
        public int GroupId { get; set; }
        public int GroupOwnerId { get; set; }
        public string GroupOwnerName { get; set; }
    }
}
