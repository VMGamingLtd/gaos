﻿#pragma warning disable 8632
namespace Gaos.Routes.Model.GameDataJson
{
    [System.Serializable]
    public class EnsureNewSlotResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public string MongoDocumentId { get; set; }
        public string MongoDocumentVersion { get; set; }

    }
}
