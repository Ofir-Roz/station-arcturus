using System;
using UnityEngine;
using SocketIOClient;
using Newtonsoft.Json;

public class WebSocketClient : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string serverUrl = "http://localhost:8000";
    [SerializeField] private float reconnectDelay = 3f;

    [Header("Status")]
    [SerializeField] private bool isConnected = false;

    // Events
    public event Action<BeaconUpdateData> OnBeaconUpdate;
    public event Action OnConnected;
    public event Action OnDisconnected;

    private SocketIOUnity socket;

    void Start()
    {
        InitializeSocket();
    }

    private void InitializeSocket()
    {
        var uri = new Uri(serverUrl);

        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Query = new System.Collections.Generic.Dictionary<string, string>
            {
                // Add any query parameters if needed
            },
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
            EIO = (EngineIO)4 // Engine.IO protocol version
        });

        // setup event listeners
        SetupEventListeners();

        // connect
        ConnectAsync();
    }

    void SetupEventListeners()
    {
        // Connection events
        socket.OnConnected += (sender, e) =>
        {
            Debug.Log("Connected to Station Arcturus Backend!");

            // Run on Unity main thread
            UnityThread.executeInUpdate(() =>
            {
                isConnected = true;
                OnConnected?.Invoke();
            });
        };

        socket.OnDisconnected += (sender, e) =>
        {
            Debug.Log("Disconnected from Station Arcturus Backend!" + e);

            // Run on Unity main thread
            UnityThread.executeInUpdate(() =>
            {
                isConnected = false;
                OnDisconnected?.Invoke();
            });
        };

        socket.OnReconnectAttempt += (sender, e) =>
        {
            Debug.Log($"Reconnecting to Station Arcturus Backend... Attempt: {e}");
        };

        // Beacon update event
        socket.On("beacon_update", response =>
        {
            try
            {
                // Socket.IO wraps the data in an array, so we get the JSON string and parse it
                string jsonString = response.ToString();

                // Remove the outer array brackets if present
                if (jsonString.StartsWith("[") && jsonString.EndsWith("]"))
                {
                    jsonString = jsonString.Substring(1, jsonString.Length - 2);
                }

                // Deserialize using Newtonsoft.Json
                var data = JsonConvert.DeserializeObject<BeaconUpdateData>(jsonString);

                // Check for null data
                if (data == null || data.beacons == null)
                {
                    Debug.LogError("Received null beacon data");
                    return;
                }

                // Invoke on Unity's main thread
                UnityThread.executeInUpdate(() =>
                {
                    OnBeaconUpdate?.Invoke(data);
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error parsing beacon update: {ex.Message}");
            }
        });


        // Connection status event
        socket.On("connection_status", response =>
        {
            try
            {
                string jsonString = response.ToString();

                // Remove the outer array brackets if present
                if (jsonString.StartsWith("[") && jsonString.EndsWith("]"))
                {
                    jsonString = jsonString.Substring(1, jsonString.Length - 2);
                }

                var data = JsonConvert.DeserializeObject<ConnectionStatus>(jsonString);

                if (data != null)
                {
                    Debug.Log($"Server: {data.message}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse connection_status: {ex.Message}");
            }
        });

        // Pong response for ping
        socket.On("pong", response =>
        {
            Debug.Log("Pong received from server");
        });
    }

    public async void ConnectAsync()
    {
        try
        {
            await socket.ConnectAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"WebSocket connection error: {ex.Message}");
            isConnected = false;
            // Retry connection after delay
            Invoke(nameof(ConnectAsync), reconnectDelay);
        }
    }

    // Public method to manually request beacon data
    public async void RequestBeaconUpdate()
    {
        if (isConnected)
        {
            await socket.EmitAsync("request_beacon_update"); // Fixed event name
        }
    }

    // Public method to send ping
    public async void SendPing()
    {
        if (isConnected)
        {
            await socket.EmitAsync("ping");
        }
    }

    void OnDestroy()
    {
        // Clean up
        if (socket != null)
        {
            socket.Disconnect();
            socket.Dispose();
        }
    }

    void OnApplicationQuit()
    {
        // Ensure disconnection on quit
        if (socket != null)
        {
            socket.Disconnect();
        }
    }

    // Public property to check connection status
    public bool IsConnected => isConnected;
}

// Data structure for beacon updates
[Serializable]
public class BeaconUpdateData
{
    public BeaconData[] beacons;
    public float time; // Changed from 'timestamp' to match backend
}

// Data structure for connection status messages
[Serializable]
public class ConnectionStatus
{
    public string status;
    public string message;
}