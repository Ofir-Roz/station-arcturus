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

    [Header("Planetary System")]
    public PlanetController planetController; // Reference to planet
    [SerializeField] private bool usePlanetaryCoordinates = false; // Enable planetary mode

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
        // Calculate position and rotation based on mode
        Vector3 position;
        Quaternion rotation;

        if (usePlanetaryCoordinates)
        {
            position = ProjectToSphereSurface(beacon);
            Vector3 surfaceNormal = GetSurfaceNormal(position);
            // Align beacon's Y-axis (up) with surface normal (outward from planet)
            // This makes the base point toward planet core
            rotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
        }
        else
        {
            position = beacon.GetPosition();
            rotation = Quaternion.identity;
        }

        GameObject beaconObj;

        if (beaconPrefab == null)
        {
            // Create a simple sphere if no prefab is set
            beaconObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beaconObj.name = beacon.id;
            beaconObj.transform.parent = beaconContainer;
            beaconObj.transform.position = position;
            beaconObj.transform.rotation = rotation;
            beaconObj.transform.localScale = Vector3.one * 0.5f;

            // Set color based on status
            Renderer renderer = beaconObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = beacon.GetStatusColor();
            }
        }
        else
        {
            // Use prefab if available
            beaconObj = Instantiate(beaconPrefab, position, rotation, beaconContainer);
            beaconObj.name = beacon.id;

            // Update visual status
            UpdateBeaconVisual(beaconObj, beacon);
        }

        // Scale beacon relative to planet in planetary mode
        if (usePlanetaryCoordinates && planetController != null)
        {
            float beaconScale = planetController.GetRadius() * 0.04f; // 4% of planet radius
            beaconObj.transform.localScale = Vector3.one * beaconScale;
        }

        beaconObjects[beacon.id] = beaconObj;

        Debug.Log($"Created beacon: {beacon.id} at position {position}");
    }

    void UpdateBeacon(BeaconData beacon)
    {
        if (beaconObjects.TryGetValue(beacon.id, out GameObject obj))
        {
            // Calculate target position and rotation based on mode
            Vector3 targetPosition;
            Quaternion targetRotation;

            if (usePlanetaryCoordinates)
            {
                targetPosition = ProjectToSphereSurface(beacon);
                Vector3 surfaceNormal = GetSurfaceNormal(targetPosition);
                // Align beacon's Y-axis (up) with surface normal (outward from planet)
                targetRotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
            }
            else
            {
                targetPosition = beacon.GetPosition();
                targetRotation = Quaternion.identity;
            }

            // Smoothly move to new position
            obj.transform.position = Vector3.Lerp(
                obj.transform.position,
                targetPosition,
                Time.deltaTime * 5f
            );

            // Smoothly rotate to new orientation
            obj.transform.rotation = Quaternion.Slerp(
                obj.transform.rotation,
                targetRotation,
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

    // Planetary Coordinate System Methods
    // Projects 3D coordinates onto sphere surface with altitude
    public Vector3 ProjectToSphereSurface(BeaconData beaconData)
    {
        // Create direction vector using ALL three coordinates for full sphere coverage
        // Transform Y (altitude 0-8) to center around 0 for full sphere coverage
        Vector3 direction = new Vector3(
            beaconData.x,
            (beaconData.y - 4f) * 10f,  // Center Y: 0→-40, 4→0, 8→+40
            beaconData.z
        );

        // Normalize to get direction on sphere
        if (direction.magnitude > 0.01f)
        {
            direction.Normalize();
        }
        else
        {
            // Handle beacon at origin - place at north pole
            direction = Vector3.up;
        }

        // Position at planet surface + small altitude offset
        float planetRadius = planetController != null ? planetController.GetRadius() : 30f;
        float altitudeOffset = beaconData.y * 0.5f; // Small offset above surface
        float radius = planetRadius + altitudeOffset;

        return direction * radius;
    }

    // Get surface normal for beacon orientation (points outward from planet center)
    public Vector3 GetSurfaceNormal(Vector3 beaconPosition)
    {
        return beaconPosition.normalized;
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