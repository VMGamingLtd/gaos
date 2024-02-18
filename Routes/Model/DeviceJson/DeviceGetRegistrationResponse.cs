#pragma warning disable 8632

using System.Data.Common;

namespace Gaos.Routes.Model.DeviceJson
{
    [System.Serializable]
    public class DeviceGetRegistrationResponse
    {
        public bool? IsError { get; set; }

        public string? ErrorMessage { get; set; }

        public bool? IsFound { get; set; }

        public int? DeviceId { get; set; }
        public string? Identification { get; set; }
        public string? PlatformType { get; set; }
        public string? BuildVersion { get; set; }

    }
}