﻿#pragma warning disable 8632
using System.Collections.Generic;

namespace Gaos.Routes.Model.ChatRoomJson
{

    [System.Serializable]
    public class ReadMessagesBackwardsResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public ResponseMessage[]? Messages { get; set; }
    }
}
