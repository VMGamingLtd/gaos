﻿#pragma warning disable 8632
namespace Gaos.Routes.Model.GroupJson
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
