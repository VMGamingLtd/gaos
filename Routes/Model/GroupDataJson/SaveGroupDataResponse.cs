#pragma warning disable 8632
using System.Collections.Generic;

namespace Gaos.Routes.Model.GroupDataJson
{
    [System.Serializable]
    public class SaveGroupDataResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public long Version { get; set; }
    }
}
