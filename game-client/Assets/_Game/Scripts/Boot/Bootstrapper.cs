using Kingdom.Core;
using Kingdom.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kingdom.Boot
{
    public sealed class Bootstrapper : MonoBehaviour
    {
        private const string BootSceneName = "Boot";
        private const string LoginSceneName = "Login";

        [SerializeField] private GameConfig gameConfig;

        public static Bootstrapper Instance { get; private set; }

        private ServiceRegistry services;

        public void Configure(GameConfig configuredGameConfig)
        {
            gameConfig = configuredGameConfig;
        }

        public static T Resolve<T>() where T : class
        {
            if (Instance == null || Instance.services == null)
                throw new System.InvalidOperationException("Game services are not ready.");
            return Instance.services.Resolve<T>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (gameConfig == null)
            {
                Debug.LogError("Bootstrapper requires a GameConfig asset.", this);
                enabled = false;
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            services = new ServiceRegistry();
            services.Register(new GameClock());
            services.Register(new ApiClient(gameConfig));
            services.Register(new GameSession());
        }

        private void Start()
        {
            if (Instance != this || SceneManager.GetActiveScene().name != BootSceneName)
                return;

            if (!Application.CanStreamedLevelBeLoaded(LoginSceneName))
            {
                Debug.LogError($"Scene '{LoginSceneName}' is missing from the active build profile.", this);
                return;
            }

            SceneManager.LoadSceneAsync(LoginSceneName, LoadSceneMode.Single);
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            services?.Dispose();
            services = null;
            Instance = null;
        }
    }
}
