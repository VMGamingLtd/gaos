using Gaos.Dbo.Model;

namespace Gaos.Routes.Model.UserJson
{
    public class UpdateUserColorsRequest
    {
        public int UserId { get; set; }
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        public UserInterfaceColors? UserInterfaceColors { get; set; }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    }
}
