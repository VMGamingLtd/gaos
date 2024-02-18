namespace Gaos.Dbo.Model
{
    [System.Serializable]
    public class Session
    {
        public int Id { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public System.DateTime ExpiresdAt { get; set; }
        public System.DateTime AccessedAt { get; set; }
    }
}
