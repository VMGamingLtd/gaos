﻿namespace Gaos.Common
{
    public class Context
    {
        public static string HTTP_CONTEXT_KEY_TOKEN_CLAIMS = "token_claims";
        public static string HTTP_CONTEXT_KEY_SESSION_ID = "session_id";
        public static string HTTP_CONTEXT_KEY_SHARED_CLIENT_SERVER_SECRET = "shared_client_server_secret"; // used for encryption, password between client and server

        public static string ROLE_PLAYER_NAME = "Player";
        public static int ROLE_PLAYER_ID = 1;
        public static string ROLE_ADMIN_NAME = "Admin";
        public static int ROLE_ADMIN_ID = 2;

        public static int TOKEN_EXPIRATION_HOURS = 100 * 365 * 24; // 100 years

        // debugging
        public static bool IS_DEBUG = false;
        public static bool IS_DEBUG_SEND_GAMEDATA_ON_SAVE = false;

    }
}
