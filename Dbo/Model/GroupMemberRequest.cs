namespace Gaos.Dbo.Model
{
    [System.Serializable]
    public class GroupMemberRequest
    {
        public int Id { get; set; }
        public int? GroupId { get; set; }
        public Groupp? Group { get; set; }
        public int? UserId { get; set; }
        public User? User { get; set; }
        public DateTime RequestDate { get; set; }
    }
}
