using System;
using System.Collections;
using System.Text;
using Kingdom.Contracts;
using Kingdom.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace Kingdom.Networking
{
    public sealed class ApiClient
    {
        private readonly GameConfig config;
        private string accessToken;

        public ApiClient(GameConfig config)
        {
            this.config = config != null ? config : throw new ArgumentNullException(nameof(config));
        }

        public void SetAccessToken(string token) => accessToken = token;

        public IEnumerator AuthenticateGuest(
            string installationId,
            string displayName,
            Action<GuestAuthData> onSuccess,
            Action<string> onError)
        {
            GuestAuthRequest body = new()
            {
                installation_id = installationId,
                display_name = displayName,
            };
            string json = JsonUtility.ToJson(body);
            using UnityWebRequest request = new($"{config.ApiBaseUrl}/v1/auth/guest", "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = config.RequestTimeoutSeconds;
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(CreateErrorMessage(request, "Authentication failed"));
                yield break;
            }

            GuestAuthResponse response = JsonUtility.FromJson<GuestAuthResponse>(request.downloadHandler.text);
            if (response?.data == null || string.IsNullOrWhiteSpace(response.data.access_token))
            {
                onError?.Invoke("Authentication returned an invalid response.");
                yield break;
            }
            accessToken = response.data.access_token;
            onSuccess?.Invoke(response.data);
        }

        public IEnumerator GetBootstrap(
            Action<BootstrapData> onSuccess,
            Action<string> onError)
        {
            using UnityWebRequest request = CreateAuthorizedGet("/v1/bootstrap");
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(CreateErrorMessage(request, "Loading player data failed"));
                yield break;
            }

            BootstrapResponse response = JsonUtility.FromJson<BootstrapResponse>(request.downloadHandler.text);
            if (response?.data?.player == null || response.data.city?.resources == null)
            {
                onError?.Invoke("Loading player data returned an invalid response.");
                yield break;
            }

            onSuccess?.Invoke(response.data);
        }

        public IEnumerator StartBuildingUpgrade(
            string buildingId,
            Action<StartBuildData> onSuccess,
            Action<string> onError)
        {
            StartBuildRequest body = new()
            {
                building_id = buildingId,
            };
            string json = JsonUtility.ToJson(body);
            using UnityWebRequest request = CreateAuthorizedJsonRequest("/v1/city/build/start", json);
            request.SetRequestHeader("Idempotency-Key", Guid.NewGuid().ToString("D"));

            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(CreateErrorMessage(request, "Starting building upgrade failed"));
                yield break;
            }

            StartBuildResponse response = JsonUtility.FromJson<StartBuildResponse>(request.downloadHandler.text);
            if (response?.data?.queue == null || response.data.resources == null)
            {
                onError?.Invoke("Starting building upgrade returned an invalid response.");
                yield break;
            }

            onSuccess?.Invoke(response.data);
        }

        public UnityWebRequest CreateAuthorizedGet(string path)
        {
            UnityWebRequest request = UnityWebRequest.Get($"{config.ApiBaseUrl}{path}");
            request.timeout = config.RequestTimeoutSeconds;
            if (!string.IsNullOrWhiteSpace(accessToken))
                request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            return request;
        }

        private UnityWebRequest CreateAuthorizedJsonRequest(string path, string json)
        {
            UnityWebRequest request = new($"{config.ApiBaseUrl}{path}", "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = config.RequestTimeoutSeconds;
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrWhiteSpace(accessToken))
                request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            return request;
        }

        private static string CreateErrorMessage(UnityWebRequest request, string fallback)
        {
            string responseBody = request.downloadHandler?.text;
            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                try
                {
                    ProblemDetails problem = JsonUtility.FromJson<ProblemDetails>(responseBody);
                    if (problem != null && !string.IsNullOrWhiteSpace(problem.code))
                    {
                        string detail = string.IsNullOrWhiteSpace(problem.detail)
                            ? problem.title
                            : problem.detail;
                        if (!string.IsNullOrWhiteSpace(detail))
                            return $"{detail} ({problem.code})";
                        return $"{fallback} ({problem.code}).";
                    }
                }
                catch (ArgumentException)
                {
                    // The server can return non-JSON proxy errors. Do not expose the raw response.
                }
            }

            if (request.responseCode > 0)
                return $"{fallback} (HTTP {request.responseCode}).";
            return $"{fallback}: check your connection and try again.";
        }
    }
}
