namespace Gaos.Routes.Model.FriendsJson
{

    [System.Serializable]
    public class GetMyFriendsRequest
    {
        public int GroupId { get; set; }
        public  int MaxCount { get; set; }
    }
}
