#pragma warning disable 8632
using System.Collections.Generic;

namespace Gaos.Routes.Model.ChatRoomJson
{
    [System.Serializable]
    public class CreateChatRoomRequest
    {
        public string? ChatRoomName { get; set; }
        public bool? IsFriedndsChatroom { get; set; }
        public bool? IsGroupChatroom { get; set; }
    }
}
