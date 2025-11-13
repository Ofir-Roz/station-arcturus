using System;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine;
using SocketIOClient;
using Newtonsoft.Json;

public class WebSocketClient : MonoBehaviour
{
    [SerializeField] private string serverUrl = "http://localhost:8000";
    
    public event Action<BeaconUpdateData> OnBeaconUpdate;
    public event Action OnConnected;
    public event Action OnDisconnected;

    private SocketIOUnity socket;

    [Header("HTTP Fallback")]
    [SerializeField] private bool useHttpFallback = true; // enable HTTP polling when socket is disconnected
    [SerializeField] private float httpPollInterval = 2.0f; // seconds

    private Coroutine pollingCoroutine;

    void Start()
    {
        var uri = new Uri(serverUrl);
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });

        socket.OnConnected += (sender, e) =>
        {
            UnityThread.executeInUpdate(() => {
                OnConnected?.Invoke();
                // stop HTTP polling when socket connected
                if (pollingCoroutine != null) { StopCoroutine(pollingCoroutine); pollingCoroutine = null; }
            });
        };

        socket.OnDisconnected += (sender, e) =>
        {
            UnityThread.executeInUpdate(() => {
                OnDisconnected?.Invoke();
                if (useHttpFallback)
                {
                    // Start HTTP polling if socket is disconnected and fallback is allowed
                    if (pollingCoroutine == null) pollingCoroutine = StartCoroutine(PollBeaconsWithHttp());
                }
            });
        };

        socket.On("beacon_update", response =>
        {
            try
            {
                string json = response.ToString();
                if (json.StartsWith("[")) json = json.Substring(1, json.Length - 2);
                
                var data = JsonConvert.DeserializeObject<BeaconUpdateData>(json);
                if (data?.beacons != null)
                {
                    UnityThread.executeInUpdate(() => OnBeaconUpdate?.Invoke(data));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Beacon update error: {ex.Message}");
            }
        });

        // Start the initial HTTP fetch before connecting, to have an immediate baseline in the UI
        StartCoroutine(FetchInitialBeacons());

        // Start HTTP polling fallback by default if enabled - stop it later when a socket connects
        if (useHttpFallback && pollingCoroutine == null)
        {
            pollingCoroutine = StartCoroutine(PollBeaconsWithHttp());
        }

        ConnectAsync();
    }

    async void ConnectAsync()
    {
        try
        {
            await socket.ConnectAsync();
            // stop polling on successful connect
            UnityThread.executeInUpdate(() => {
                if (pollingCoroutine != null) { StopCoroutine(pollingCoroutine); pollingCoroutine = null; }
            });
        }
        catch (Exception ex)
        {
            Debug.LogError($"Connection error: {ex.Message}");
            Invoke(nameof(ConnectAsync), 3f);
        }
    }

    void OnDestroy()
    {
        socket?.Disconnect();
        socket?.Dispose();
    }

    IEnumerator FetchInitialBeacons()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(serverUrl + "/beacons"))
        {
            yield return www.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool isError = www.result != UnityWebRequest.Result.Success;
#else
            bool isError = www.isNetworkError || www.isHttpError;
#endif
            if (!isError)
            {
                try
                {
                    string json = www.downloadHandler.text;
                    var response = JsonConvert.DeserializeObject<BeaconResponse>(json);
                    if (response != null && response.beacons != null)
                    {
                        var updateData = new BeaconUpdateData { beacons = response.beacons, time = response.time };
                        UnityThread.executeInUpdate(() => OnBeaconUpdate?.Invoke(updateData));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to parse initial beacons: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"HTTP initial fetch failed: {www.error}");
            }
        }
    }

    IEnumerator PollBeaconsWithHttp()
    {
        while (true)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(serverUrl + "/beacons"))
            {
                yield return www.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                bool isError = www.result != UnityWebRequest.Result.Success;
#else
                bool isError = www.isNetworkError || www.isHttpError;
#endif
                if (!isError)
                {
                    try
                    {
                        string json = www.downloadHandler.text;
                        var response = JsonConvert.DeserializeObject<BeaconResponse>(json);
                        if (response != null && response.beacons != null)
                        {
                            var updateData = new BeaconUpdateData { beacons = response.beacons, time = response.time };
                            UnityThread.executeInUpdate(() => OnBeaconUpdate?.Invoke(updateData));
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to parse polled beacons: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"HTTP poll fetch failed: {www.error}");
                }
            }
            yield return new WaitForSeconds(httpPollInterval);
        }
    }
}

[Serializable]
public class BeaconUpdateData
{
    public BeaconData[] beacons;
    public float time;
}