#pragma warning disable 8632
using System.Collections.Generic;

namespace Gaos.Routes.Model.GroupDataJson
{
    [System.Serializable]
    public class GetGroupDataResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public string? Id { get; set; }
        public string? Version { get; set; }
        public string? GroupDataJson { get; set; }
    }
}
