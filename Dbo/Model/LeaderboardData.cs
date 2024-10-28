namespace Gaos.Dbo.Model
{
    public class LeaderboardData
    {
        public int Id { get; set; }
        public int UserId { get; set; }
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        public string? Username { get; set; }
        public string? Country { get; set; }
        public int? Hours { get; set; }
        public int? Minutes { get; set; }
        public int? Seconds { get; set; }
        public int? Score { get; set; }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    }
}
