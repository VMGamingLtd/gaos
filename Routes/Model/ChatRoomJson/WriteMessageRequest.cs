#pragma warning disable 8632
using System.Collections.Generic;

namespace Gaos.Routes.Model.ChatRoomJson
{
    [System.Serializable]
    public class WriteMessageRequest
    {
        public int ChatRoomId { get; set; }
        public string? Message  { get; set; }

    }
}
