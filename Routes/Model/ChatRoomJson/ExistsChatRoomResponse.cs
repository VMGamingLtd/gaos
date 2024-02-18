#pragma warning disable 8632
using System.Collections.Generic;

namespace Gaos.Routes.Model.ChatRoomJson
{
    [System.Serializable]
    public class ExistsChatRoomResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public bool? IsExists { get; set; }
        public int ChatRoomId { get; set; }
    }
}
