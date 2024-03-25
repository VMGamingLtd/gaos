#pragma warning disable 8632, 8618

namespace Gaos.Routes.Model.GameDataJson
{
    [System.Serializable]
    public class DeleteSlotResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
