namespace Gaos.wsrv
{
    public enum NamespaceIds
    {
        WebSocket = 1,
        UnityBrowserChannel = 2,
        Group = 3,
        Gaos = 4
    }

    public enum WebSocketClassIds
    {
        PingPong = 1,
        Authenticate = 2
    }

    public enum WebSocketPingPongMethodIds
    {
        Ping = 1,
        Pong = 2
    }

    public enum WebSocketAuthenticateMethodIds
    {
        AuthenticateRequest = 1,
        AuthenticateResponse = 2
    }

    public enum GroupClassIds
    {
        Broadcast = 1
    }

    public enum GaosClassIds
    {
        Broadcast = 1
    }

    public enum GaosBroadcastMethodIds
    {
        GroupCreditsChange = 1
    }
}
