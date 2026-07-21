using System;

namespace Kingdom.Contracts
{
    [Serializable]
    public sealed class GuestAuthRequest
    {
        public string installation_id;
        public string display_name;
    }

    [Serializable]
    public sealed class GuestAuthData
    {
        public string account_id;
        public string player_id;
        public string access_token;
        public string refresh_token;
        public string access_token_expires_at;
    }

    [Serializable]
    public sealed class GuestAuthResponse
    {
        public GuestAuthData data;
    }

    [Serializable]
    public sealed class BootstrapResources
    {
        public string food;
        public string wood;
        public string iron;
        public string silver;
        public string gold;
    }

    [Serializable]
    public sealed class BootstrapBuilding
    {
        public string id;
        public string slot_key;
        public string building_type;
        public int level;
    }

    [Serializable]
    public sealed class BootstrapQueueJob
    {
        public string id;
        public string job_type;
        public string ref_id;
        public string finish_at;
        public string status;
    }

    [Serializable]
    public sealed class BootstrapPlayer
    {
        public string id;
        public string kingdom_id;
        public string display_name;
        public string power;
    }

    [Serializable]
    public sealed class BootstrapCity
    {
        public string id;
        public int x;
        public int y;
        public BootstrapResources resources;
        public BootstrapBuilding[] buildings;
        public BootstrapQueueJob[] active_queues;
    }

    [Serializable]
    public sealed class BootstrapMarch
    {
        public string id;
        public string march_type;
        public string status;
        public string arrive_at;
    }

    [Serializable]
    public sealed class BootstrapData
    {
        public string server_time;
        public BootstrapPlayer player;
        public BootstrapCity city;
        public BootstrapMarch[] active_marches;
    }

    [Serializable]
    public sealed class BootstrapResponse
    {
        public BootstrapData data;
    }

    [Serializable]
    public sealed class StartBuildRequest
    {
        public string building_id;
    }

    [Serializable]
    public sealed class StartBuildData
    {
        public BootstrapQueueJob queue;
        public BootstrapResources resources;
    }

    [Serializable]
    public sealed class StartBuildResponse
    {
        public StartBuildData data;
    }

    [Serializable]
    public sealed class ProblemDetails
    {
        public string type;
        public string title;
        public int status;
        public string code;
        public string detail;
        public string instance;
        public string correlation_id;
    }

    [Serializable]
    public sealed class RealtimeEnvelope
    {
        public string event_id;
        public string type;
        public string sequence;
        public string occurred_at;
        public string payload;
    }
}
