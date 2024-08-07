namespace Gaos.Routes.Model.FriendsJson
{
    [System.Serializable]
    public class GroupMembersListUser
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool? IsOwner { get; set; }
    }

    [System.Serializable]
    public class GetGroupMembersResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public GroupMembersListUser[]? Users { get; set; }
    }
}
