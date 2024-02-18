#pragma warning disable 8632
using System.Collections.Generic;

namespace Gaos.Routes.Model.ChatRoomJson
{
    [System.Serializable]
    public class ResponseMessage
    {
        public int MessageId { get; set; }
        public string? Message { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }

    }

}
