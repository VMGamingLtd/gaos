namespace Gaos.Common
{
    public class Context
    {
        public static string HTTP_CONTEXT_KEY_TOKEN_CLAIMS = "token_claims";
        public static string HTTP_CONTEXT_KEY_SESSION_ID = "session_id";

        public static string ROLE_PLAYER_NAME = "Player";
        public static int ROLE_PLAYER_ID = 1;
        public static string ROLE_ADMIN_NAME = "Admin";
        public static int ROLE_ADMIN_ID = 2;

        public static int TOKEN_EXPIRATION_HOURS = 100 * 365 * 24; // 100 years
    }
}
