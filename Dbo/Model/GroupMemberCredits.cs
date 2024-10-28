namespace Gaos.Dbo.Model
{
    [System.Serializable]
    public class GroupCredits
    {
        public int Id { get; set; }
        public float Credits { get; set; }
        public int GroupId { get; set; }
        public Groupp Group { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
