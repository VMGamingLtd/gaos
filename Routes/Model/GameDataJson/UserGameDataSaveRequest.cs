﻿#pragma warning disable 8632
using System.Collections.Generic;

namespace Gaos.Routes.Model.GameDataJson
{
    [System.Serializable]
    public class UserGameDataSaveRequest
    {
        public int  UserId  { get; set; }
        public int  SlotId  { get; set; }
        public long Version  { get; set; }
        public string? GameDataJson  { get; set; }
        public bool? IsGameDataDiff  { get; set; }
        public string? GameDataDiffBase  { get; set; }
    }
}
