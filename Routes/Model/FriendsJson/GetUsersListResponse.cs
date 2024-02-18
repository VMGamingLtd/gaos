#pragma warning disable 8632
using System.Collections.Generic;

namespace Gaos.Routes.Model.FriendsJson
{
    [System.Serializable]
    public class UsersListUser
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    [System.Serializable]
    public class GetUsersListResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public UsersListUser[]? Users { get; set; }
    }
}
