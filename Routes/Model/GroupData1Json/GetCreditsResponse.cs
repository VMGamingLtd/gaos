namespace Gaos.Routes.Model.GroupData1Json
{
    [System.Serializable]
    public class GetCreditsResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public float Credits { get; set; }
    }
}
