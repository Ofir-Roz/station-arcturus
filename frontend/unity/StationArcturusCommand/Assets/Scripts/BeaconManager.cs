using System.Collections.Generic;
using UnityEngine;

public class BeaconManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private GameObject beaconPrefab;
    public PlanetController planetController;
    
    [Header("Projection Settings")]
    [SerializeField] private float mapExtent = 100f;
    [SerializeField] private float altitudeScale = 0.25f;
    
    [Header("Status")]
    public bool isConnected = false;
    
    private Dictionary<string, GameObject> beaconObjects = new Dictionary<string, GameObject>();
    private Dictionary<string, BeaconData> currentBeacons = new Dictionary<string, BeaconData>();
    private WebSocketClient webSocketClient;
    private Transform beaconContainer;

    void Start()
    {
        GameObject container = new GameObject("BeaconContainer");
        beaconContainer = container.transform;

        webSocketClient = gameObject.AddComponent<WebSocketClient>();
        webSocketClient.OnBeaconUpdate += HandleBeaconUpdate;
        webSocketClient.OnConnected += () => isConnected = true;
        webSocketClient.OnDisconnected += () => isConnected = false;
    }

    void HandleBeaconUpdate(BeaconUpdateData data)
    {
        UpdateBeacons(data.beacons);
    }

    void UpdateBeacons(BeaconData[] beacons)
    {
        if (beacons == null) return;

        // Track which beacons are present in the current update using a set
        HashSet<string> seenBeacons = new HashSet<string>();
        foreach (BeaconData beacon in beacons)
        {
            seenBeacons.Add(beacon.id);

            if (currentBeacons.ContainsKey(beacon.id))
            {
                UpdateBeacon(beacon);
            }
            else
            {
                CreateBeacon(beacon);
            }

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
        Vector3 position = CalculatePosition(beacon);
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, position.normalized);

        GameObject beaconObj = Instantiate(beaconPrefab, position, rotation, beaconContainer);
        beaconObj.name = beacon.id;

        // Scale relative to planet
        float scale = planetController.GetRadius() * 0.04f;
        beaconObj.transform.localScale = new Vector3(scale * 0.6f, scale * 3f, scale * 0.6f);

        beaconObjects[beacon.id] = beaconObj;
        UpdateBeaconVisual(beaconObj, beacon);
    }

    void UpdateBeacon(BeaconData beacon)
    {
        if (!beaconObjects.TryGetValue(beacon.id, out GameObject obj)) return;

        Vector3 targetPosition = CalculatePosition(beacon);
        Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, targetPosition.normalized);

        obj.transform.position = Vector3.Lerp(obj.transform.position, targetPosition, Time.deltaTime * 5f);
        obj.transform.rotation = Quaternion.Slerp(obj.transform.rotation, targetRotation, Time.deltaTime * 5f);

        // Safety: ensure beacon never goes below planet surface
        float minRadius = planetController.GetRadius();
        if (obj.transform.position.magnitude < minRadius)
        {
            obj.transform.position = obj.transform.position.normalized * minRadius;
        }

        UpdateBeaconVisual(obj, beacon);
    }

    void UpdateBeaconVisual(GameObject obj, BeaconData beacon)
    {
        BeaconVisual visual = obj.GetComponent<BeaconVisual>();
        if (visual != null)
        {
            visual.SetStatus(beacon.status);
        }
    }

    void RemoveBeacon(string id)
    {
        if (beaconObjects.TryGetValue(id, out GameObject obj))
        {
            Destroy(obj);
            beaconObjects.Remove(id);
            currentBeacons.Remove(id);
        }
    }

    Vector3 CalculatePosition(BeaconData beacon)
    {
        float planetRadius = planetController.GetRadius();
        
        // Map backend coordinates to spherical coordinates
        float xNorm = Mathf.Clamp(beacon.x / mapExtent, -1f, 1f);
        float zNorm = Mathf.Clamp(beacon.z / mapExtent, -1f, 1f);
        
        float lon = xNorm * Mathf.PI;
        float lat = zNorm * Mathf.PI * 0.5f;
        
        Vector3 direction = new Vector3(
            Mathf.Cos(lat) * Mathf.Cos(lon),
            Mathf.Sin(lat),
            Mathf.Cos(lat) * Mathf.Sin(lon)
        ).normalized;
        
        // Clamp altitude to minimum of 0 (can't go below surface)
        float altitude = Mathf.Max(0f, beacon.y * altitudeScale);
        float radius = planetRadius + altitude;
        
        return direction * radius;
    }

    public Dictionary<string, int> GetBeaconStats()
    {
        Dictionary<string, int> stats = new Dictionary<string, int>
        {
            { "total", currentBeacons.Count },
            { "active", 0 },
            { "damaged", 0 },
            { "offline", 0 }
        };

        foreach (BeaconData beacon in currentBeacons.Values)
        {
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
        if (webSocketClient != null)
        {
            webSocketClient.OnBeaconUpdate -= HandleBeaconUpdate;
        }
    }
}