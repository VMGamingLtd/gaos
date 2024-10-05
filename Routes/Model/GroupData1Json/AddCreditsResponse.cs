namespace Gaos.Routes.Model.GroupData1Json
{
    [System.Serializable]
    public class AddCreditsResponse
    {
        public bool? IsError { get; set; }
        public string? ErrorMessage { get; set; }

        public float Credits { get; set; }
    }
}
