using System;
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

    void Start()
    {
        var uri = new Uri(serverUrl);
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });

        socket.OnConnected += (sender, e) =>
        {
            UnityThread.executeInUpdate(() => OnConnected?.Invoke());
        };

        socket.OnDisconnected += (sender, e) =>
        {
            UnityThread.executeInUpdate(() => OnDisconnected?.Invoke());
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

        ConnectAsync();
    }

    async void ConnectAsync()
    {
        try
        {
            await socket.ConnectAsync();
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
}

[Serializable]
public class BeaconUpdateData
{
    public BeaconData[] beacons;
    public float time;
}