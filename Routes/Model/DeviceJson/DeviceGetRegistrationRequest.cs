#pragma warning disable 8632
namespace Gaos.Routes.Model.DeviceJson
{

    [System.Serializable]
    public class DeviceGetRegistrationRequest
    {
        public string? Identification { get; set; }
        public string? PlatformType { get; set; }

    }
}