﻿#pragma warning disable 8632
namespace Gaos.Routes.Model.GroupJson
{
    [System.Serializable]
    public class GroupMembersListUser
    {
        public int UserId { get; set; }
        public string? UserName { get; set; }
    }

    [System.Serializable]
    public class GetMyGroupMembersResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public GroupMembersListUser[]? Users { get; set; }
        public int? TotalCount { get; set; }
    }
}
