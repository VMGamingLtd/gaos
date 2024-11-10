namespace Gaos.Dbo.Model
{
    public class NewsData
    {
        public int Id { get; set; }
        public string ImageName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Headline { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
    }
}
