using UnityEngine;

namespace Kingdom.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Kingdom/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [SerializeField] private string apiBaseUrl = "http://localhost:3000";
        [SerializeField] private string realtimeBaseUrl = "ws://localhost:3001/v1/realtime";
        [SerializeField] private int requestTimeoutSeconds = 15;

        public string ApiBaseUrl => apiBaseUrl.TrimEnd('/');
        public string RealtimeBaseUrl => realtimeBaseUrl;
        public int RequestTimeoutSeconds => Mathf.Clamp(requestTimeoutSeconds, 5, 60);
    }
}

