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

    [Header("Projection Tuning")]
    [SerializeField] private float mapExtent = 100f; // backend coordinate ranges
    [SerializeField] private float altitudeScale = 0.25f; // scale factor for backend altitude -> in-world offset
    [SerializeField] private float minSurfaceBuffer = 0.01f; // minimum buffer above planet surface
    [SerializeField] private float atmosphereSafetyMargin = 0.90f; // Keep beacons slightly under atmosphere shell

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
            // Make beacon thinner and taller: x=0.3, y=1.5, z=0.3
            beaconObj.transform.localScale = new Vector3(0.3f, 1.5f, 0.3f);

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
            float baseScale = planetController.GetRadius() * 0.04f; // 4% of planet radius
            // Apply thin and tall proportions: thin on X/Z, tall on Y
            beaconObj.transform.localScale = new Vector3(baseScale * 0.6f, baseScale * 3f, baseScale * 0.6f);
        }

        // Ensure the visual sits above the planet's surface by offsetting by half the object's world height
        // Reuse surfaceNormal if available, otherwise compute
        Vector3 surfaceNormalForOffset = GetSurfaceNormal(position);
        float halfHeight = GetWorldHalfHeight(beaconObj);
        if (halfHeight > 0f)
        {
            position += surfaceNormalForOffset * halfHeight;
            beaconObj.transform.position = position; // reapply position after adjusting for height
        }

        // Safety clamp to prevent penetrating the surface
        float planetRadius = planetController != null ? planetController.GetRadius() : 30f;
        float minRadius = planetRadius + minSurfaceBuffer; // use configurable buffer above surface
        if (beaconObj.transform.position.magnitude < minRadius)
        {
            beaconObj.transform.position = beaconObj.transform.position.normalized * minRadius;
        }

        // Final sanity check - if it's still below the surface something is wrong
        if (beaconObj.transform.position.magnitude < planetRadius)
        {
            Debug.LogWarning($"Beacon {beacon.id} computed inside planet radius: r={beaconObj.transform.position.magnitude} < planetRadius={planetRadius}");
            // Force it outside minimally
            beaconObj.transform.position = beaconObj.transform.position.normalized * minRadius;
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
                // add half world height so the mesh's base sits on surface rather than its center
                float halfHeight = GetWorldHalfHeight(obj);
                if (halfHeight > 0f)
                {
                    targetPosition += surfaceNormal * halfHeight;
                }
                // clamp to avoid intersections
                float planetRadius = planetController != null ? planetController.GetRadius() : 30f;
                float minRadius = planetRadius + minSurfaceBuffer;
                if (targetPosition.magnitude < minRadius)
                {
                    targetPosition = targetPosition.normalized * minRadius;
                }
                // Sanity check
                if (targetPosition.magnitude < planetController.GetRadius())
                {
                    Debug.LogWarning($"Target position for beacon {beacon.id} is inside the planet radius: r={targetPosition.magnitude}");
                    targetPosition = targetPosition.normalized * minRadius;
                }
            }
            else
            {
                targetPosition = beacon.GetPosition();
                targetRotation = Quaternion.identity;
            }

            // If object is currently inside the planet, snap it out to the minimum radius
            float currentMinRadius = (planetController != null ? planetController.GetRadius() : 30f) + minSurfaceBuffer;
            if (obj.transform.position.magnitude < currentMinRadius)
            {
                obj.transform.position = obj.transform.position.normalized * currentMinRadius;
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
    // Projects flat x,z coordinates to a point ON the sphere surface; altitude is applied as radial offset.
    // - Backend sends: x and z in a flat coordinate system (expected roughly -100..100)
    // - Altitude (y) is an offset above the surface and does not modify surface direction
    // We map x->longitude, z->latitude so the flat coordinates become spherical coordinates
    public Vector3 ProjectToSphereSurface(BeaconData beaconData)
    {
        // Defensive defaults
        float planetRadius = planetController != null ? planetController.GetRadius() : 30f;
        float altitude = Mathf.Max(0f, beaconData.y); // ensure altitude is non-negative

        // Map planar x,z ranges to [-1,1] expectations for longitude/latitude mapping
        // The backend currently uses roughly -100..100 for x/z. Keep this constant for consistency.
        float xNorm = Mathf.Clamp(beaconData.x / mapExtent, -1f, 1f);
        float zNorm = Mathf.Clamp(beaconData.z / mapExtent, -1f, 1f);

        // Longitude: full circle mapping from [-1,1] -> [-180,180]
        float lon = xNorm * 180f * Mathf.Deg2Rad;
        // Latitude: clamp to avoid going past poles [-90,90]
        float lat = zNorm * 90f * Mathf.Deg2Rad;

        // Convert spherical angles to cartesian direction
        Vector3 direction = new Vector3(
            Mathf.Cos(lat) * Mathf.Cos(lon),
            Mathf.Sin(lat),
            Mathf.Cos(lat) * Mathf.Sin(lon)
        );

        // Normalize direction to be safe
        direction.Normalize();

        // Radius: planet surface + altitude (radial offset)
        float altitudeOffset = altitude * altitudeScale; // scale altitude to world units; smaller to keep under atmosphere
        // If planetController has atmosphere, clamp to be under the atmosphere
        if (planetController != null)
        {
            // Use the already-computed planetRadius variable (declared at the top of this method) to avoid shadowing
            float atmosphereOuterRadius = planetRadius * planetController.atmosphereThickness;
            float maxAltitudeOffset = (atmosphereOuterRadius - planetRadius) * atmosphereSafetyMargin;
            altitudeOffset = Mathf.Min(altitudeOffset, maxAltitudeOffset);
        }
        float radius = planetRadius + altitudeOffset;

        return direction * radius;
    }

    // Get surface normal for beacon orientation (points outward from planet center)
    public Vector3 GetSurfaceNormal(Vector3 beaconPosition)
    {
        return beaconPosition.normalized;
    }

    // Compute half the height of the object's visible mesh in world space so that we can offset
    // the object's center so the base sits on the planet surface (avoids sinking into the planet).
    private float GetWorldHalfHeight(GameObject obj)
    {
        if (obj == null) return 0f;
        // Combine bounds from all renderers in the object (and children) so complex prefabs are measured properly.
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            Bounds combined = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combined.Encapsulate(renderers[i].bounds);
            }
            return combined.size.y * 0.5f;
        }

        // As a conservative fallback use lossyScale.y * 0.5f (assuming model height of 1 unit)
        return obj.transform.lossyScale.y * 0.5f;
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