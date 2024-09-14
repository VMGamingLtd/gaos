namespace Gaos.Dbo.Model
{
    [System.Serializable]
    public class UserInterfaceColors
    {
        public int Id { get; set; }
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        public int? UserId { get; set; }
        public User? User { get; set; }
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public string? BackgroundColor { get; set; }
        public string? SecondaryBackgroundColor { get; set; }
        public string? NegativeColor { get; set; }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    }
}
