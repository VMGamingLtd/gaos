#pragma warning disable 8632
using System.Collections.Generic;

namespace Gaos.Routes.Model.ChatRoomJson
{
    [System.Serializable]
    public class ReadMessagesBackwardsRequest
    {
        public int ChatRoomId { get; set; }
        public int LastMessageId  { get; set; }
        public int Count  { get; set; }


    }
}
