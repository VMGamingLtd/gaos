#pragma warning disable 8632
using System.Collections.Generic;

namespace Gaos.Routes.Model.ChatRoomJson
{
    [System.Serializable]
    public class WriteMessageResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public int MinMessageId { get; set; }
        public int MaxMessageId { get; set; }
        public int MessageCount { get; set; }
    }
}
