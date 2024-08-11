#pragma warning disable 8632
namespace Gaos.Dbo.Model
{
    [System.Serializable]
    public class Groupp
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? OwnerId { get; set; }
        public User? Owner { get; set; }
    }
}
