using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Unity.VisualScripting;

public class BeaconManager : MonoBehaviour
{
    [Header("Backend Configuration")]
    [SerializeField] private string backendUrl = "http://localhost:8000";

    [Header("Connection Mode")]
    [SerializeField] private bool useWebSocket = true; // Default to WebSocket
    [SerializeField] private float pollInterval = 2f; // Fallback polling interval

    [Header("Beacon Visualization")]
    [SerializeField] private GameObject beaconPrefab;
    [SerializeField] private Transform beaconContainer;

    [Header("Status")]
    public bool isConnected = false; // Public for UI
    [SerializeField] private int beaconCount = 0;

    // Store beacon GameObjects by ID
    private Dictionary<string, GameObject> beaconObjects = new Dictionary<string, GameObject>();

    // Store beacon data
    private Dictionary<string, BeaconData> currentBeacons = new Dictionary<string, BeaconData>();

    // Reference to WebSocket client
    private WebSocketClient webSocketClient;

    void Start()
    {
        Debug.Log("BeaconManager starting...");

        // Create container for beacons if not set
        if (beaconContainer == null)
        {
            GameObject container = new GameObject("BeaconContainer");
            beaconContainer = container.transform;
        }

        // Try to get or add WebSocket client component
        webSocketClient = GetComponent<WebSocketClient>();
        if (webSocketClient == null && useWebSocket)
        {
            // Add WebSocketClient component if not present
            webSocketClient = gameObject.AddComponent<WebSocketClient>();
        }

        if (useWebSocket && webSocketClient != null)
        {
            Debug.Log("Using WebSocket connection mode");

            // Subscribe to WebSocket events
            webSocketClient.OnBeaconUpdate += HandleBeaconUpdate;
            webSocketClient.OnConnected += HandleConnected;
            webSocketClient.OnDisconnected += HandleDisconnected;
        }
        else
        {
            Debug.Log("Using HTTP polling mode");
            StartCoroutine(PollForUpdates());
        }
    }

    // WebSocket event handlers
    void HandleBeaconUpdate(BeaconUpdateData data)
    {
        Debug.Log($"Received beacon update via WebSocket: {data.beacons.Length} beacons");
        UpdateBeacons(data.beacons);
    }

    void HandleConnected()
    {
        isConnected = true;
        Debug.Log("Connected to backend!");
    }

    void HandleDisconnected()
    {
        isConnected = false;
        Debug.LogWarning("Disconnected from backend!");
    }

    // HTTP Polling fallback (existing code)
    IEnumerator PollForUpdates()
    {
        while (true)
        {
            yield return StartCoroutine(FetchBeaconData());
            yield return new WaitForSeconds(pollInterval);
        }
    }

    IEnumerator FetchBeaconData()
    {
        string url = $"{backendUrl}/beacons";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                isConnected = true;
                string jsonResponse = request.downloadHandler.text;

                try
                {
                    BeaconResponse response = JsonConvert.DeserializeObject<BeaconResponse>(jsonResponse);
                    UpdateBeacons(response.beacons);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to parse beacon data: {e.Message}");
                }
            }
            else
            {
                isConnected = false;
                Debug.LogError($"Failed to fetch beacon data: {request.error}");
            }
        }
    }

    void UpdateBeacons(BeaconData[] beacons)
    {
        if (beacons == null) return;

        beaconCount = beacons.Length;

        // Track which beacons we've seen this update
        HashSet<string> seenBeacons = new HashSet<string>();

        foreach (BeaconData beacon in beacons)
        {
            seenBeacons.Add(beacon.id);

            if (currentBeacons.ContainsKey(beacon.id))
            {
                // Update existing beacon
                UpdateBeacon(beacon);
            }
            else
            {
                // Create new beacon
                CreateBeacon(beacon);
            }

            // Store/update beacon data
            currentBeacons[beacon.id] = beacon;
        }

        // Remove beacons that weren't in this update
        List<string> toRemove = new List<string>();
        foreach (string id in currentBeacons.Keys)
        {
            if (!seenBeacons.Contains(id))
            {
                toRemove.Add(id);
            }
        }

        foreach (string id in toRemove)
        {
            RemoveBeacon(id);
        }
    }

    void CreateBeacon(BeaconData beacon)
    {
        if (beaconPrefab == null)
        {
            // Create a simple sphere if no prefab is set
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = beacon.id;
            sphere.transform.parent = beaconContainer;
            sphere.transform.position = beacon.GetPosition();
            sphere.transform.localScale = Vector3.one * 0.5f;

            // Set color based on status
            Renderer renderer = sphere.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = beacon.GetStatusColor();
            }

            beaconObjects[beacon.id] = sphere;
        }
        else
        {
            // Use prefab if available
            GameObject obj = Instantiate(beaconPrefab, beacon.GetPosition(), Quaternion.identity, beaconContainer);
            obj.name = beacon.id;
            beaconObjects[beacon.id] = obj;

            // Update visual status
            UpdateBeaconVisual(obj, beacon);
        }

        Debug.Log($"Created beacon: {beacon.id} at position {beacon.GetPosition()}");
    }

    void UpdateBeacon(BeaconData beacon)
    {
        if (beaconObjects.TryGetValue(beacon.id, out GameObject obj))
        {
            // Smoothly move to new position
            obj.transform.position = Vector3.Lerp(
                obj.transform.position,
                beacon.GetPosition(),
                Time.deltaTime * 5f
            );

            // Update visual status
            UpdateBeaconVisual(obj, beacon);
        }
    }

    void UpdateBeaconVisual(GameObject obj, BeaconData beacon)
    {
        // Try to use BeaconVisual component
        BeaconVisual visual = obj.GetComponent<BeaconVisual>();
        if (visual != null)
        {
            visual.SetStatus(beacon.status);
        }
        else
        {
            // Fallback to simple color change
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = beacon.GetStatusColor();
            }
        }
    }

    void RemoveBeacon(string id)
    {
        if (beaconObjects.TryGetValue(id, out GameObject obj))
        {
            Destroy(obj);
            beaconObjects.Remove(id);
            currentBeacons.Remove(id);
            Debug.Log($"Removed beacon: {id}");
        }
    }

    // Public method to get beacon statistics
    public Dictionary<string, int> GetBeaconStats()
    {
        Dictionary<string, int> stats = new Dictionary<string, int>
        {
            { "total", 0 },
            { "active", 0 },
            { "damaged", 0 },
            { "offline", 0 }
        };

        foreach (BeaconData beacon in currentBeacons.Values)
        {
            stats["total"]++;
            string status = beacon.status.ToLower();
            if (stats.ContainsKey(status))
            {
                stats[status]++;
            }
        }

        return stats;
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (webSocketClient != null)
        {
            webSocketClient.OnBeaconUpdate -= HandleBeaconUpdate;
            webSocketClient.OnConnected -= HandleConnected;
            webSocketClient.OnDisconnected -= HandleDisconnected;
        }
    }
}