#pragma warning disable 8632
namespace Gaos.Routes.Model.DeviceJson
{

    [System.Serializable]
    public class DeviceRegisterRequest
    {
        public string? Identification { get; set; }

        public string? PlatformType { get; set; }
        public string? BuildVersion { get; set; }

        public int? ecdhContext { get; set; }
        public string? ecdhPublicKey { get; set; }

    }
}