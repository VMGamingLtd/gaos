#pragma warning disable 8632
namespace Gaos.Routes.Model.DeviceJson
{
    [System.Serializable]
    public class DeviceRegisterResponseUserSlot
    {
        public int MongoDocumentId { get; set; }
        public int SlotId { get; set; }
    }

    [System.Serializable]
    public class DeviceRegisterResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public int DeviceId { get; set; }
        public string? Identification { get; set; }
        public string? PlatformType { get; set; }
        public string? BuildVersion { get; set; }

        public Dbo.Model.User? User { get; set; }
        public Dbo.Model.JWT? JWT { get; set; }

        public List<DeviceRegisterResponseUserSlot>? UserSlots { get; set; }

    }
}