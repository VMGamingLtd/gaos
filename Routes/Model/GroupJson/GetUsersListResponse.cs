#pragma warning disable 8632
using System.Collections.Generic;

namespace Gaos.Routes.Model.GroupJson
{
    [System.Serializable]
    public class UsersListUser
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool? IsFriend { get; set; }
        public bool? IsFriendRequest { get; set; }
    }

    [System.Serializable]
    public class GetUsersListResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public UsersListUser[]? Users { get; set; }
    }
}
