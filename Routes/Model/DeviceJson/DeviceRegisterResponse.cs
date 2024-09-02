#pragma warning disable 8632, 8618
namespace Gaos.Routes.Model.DeviceJson
{

    [System.Serializable]
    public class DeviceRegisterResponseUserSlot
    {
        public string MongoDocumentId { get; set; }
        public int MongoDocumentVersion { get; set; }
        public int SlotId { get; set; }

        public string UserName { get; set; }
        public int Seconds { get; set; } 
        public int Minutes { get; set; } 
        public int Hours { get; set; } 
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

        public DeviceRegisterResponseUserSlot[]? UserSlots { get; set; }

    }
}