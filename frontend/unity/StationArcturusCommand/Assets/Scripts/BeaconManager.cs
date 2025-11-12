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
    [SerializeField] private float pollInterval = 2f; // poll every 2 seconds initially

    [Header("Beacon Visualization")]
    [SerializeField] private GameObject beaconPrefab;// we'll create this next
    [SerializeField] private Transform beaconContainer; // Parent for all beacons

    [Header("Status")]
    [SerializeField] private bool isConnected = false;
    [SerializeField] private int beaconCount = 0;

    // Store beacon GameObjects by their ID
    private Dictionary<string, GameObject> beaconObjects = new Dictionary<string, GameObject>();

    // Store beacon data
    private Dictionary<string, BeaconData> currentBeacons = new Dictionary<string, BeaconData>();

    void Start()
    {
        Debug.Log("Starting Beacon Manager...");

        // Create beacon container if not assigned
        if (beaconContainer == null)
        {
            GameObject container = new GameObject("BeaconContainer");
            beaconContainer = container.transform;
        }

        // Start polling for updates
        StartCoroutine(PollForUpdates());
    }

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
            // Send the request
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                isConnected = true;

                // Parse the response
                string jsonResponse = request.downloadHandler.text;
                Debug.Log($"Received Beacon Data: {jsonResponse}");

                try
                {
                    // parse JSON response
                    BeaconResponse beaconResponse = JsonConvert.DeserializeObject<BeaconResponse>(jsonResponse);
                    UpdateBeacons(beaconResponse.beacons);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error parsing beacon data: {ex.Message}");
                }
            }
            else
            {
                isConnected = false;
                Debug.LogError($"Error fetching beacon data: {request.error}");
            }
        }
    }

    void UpdateBeacons(BeaconData[] beacons)
    {
        if (beacons == null) return;

        beaconCount = beacons.Length;

        // track which beacons we've seen this update
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

            // store/update beacon data
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
            sphere.transform.localScale = Vector3.one * 0.5f; // Make it smaller

            // Set color based on status
            Renderer renderer = sphere.GetComponent<Renderer>();
            renderer.material.color = beacon.GetStatusColor();

            beaconObjects[beacon.id] = sphere;
        }
        else
        {
            // Use prefab if avilable
            GameObject obj = Instantiate(beaconPrefab, beacon.GetPosition(), Quaternion.identity, beaconContainer);
            obj.name = beacon.id;
            beaconObjects[beacon.id] = obj;

            // Update visual status
            UpdateBeaconVisuals(obj, beacon);
        }

        Debug.Log($"Created beacon: {beacon.id}");
    }

    void UpdateBeacon(BeaconData beacon)
    {
        if (beaconObjects.TryGetValue(beacon.id, out GameObject obj))
        {
            // smoothly move to new position
            obj.transform.position = Vector3.Lerp(
                obj.transform.position,
                beacon.GetPosition(),
                Time.deltaTime * 5f     // adjust speed as needed
            );

            // Update visual status
            UpdateBeaconVisuals(obj, beacon);
        }
    }

    void UpdateBeaconVisuals(GameObject obj, BeaconData beacon)
    {
        // Try to use BeaconVisual component if available
        BeaconVisual visual = obj.GetComponent<BeaconVisual>();
        if (visual != null)
        {
            visual.SetStatus(beacon.status);
        }
        else
        {
            // Fallback to setting color directly on Renderer
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

        foreach (var beacon in currentBeacons.Values)
        {
            stats["total"]++;
            if (stats.ContainsKey(beacon.status.ToLower()))
            {
                stats[beacon.status.ToLower()]++;
            }
        }

        return stats;
    }
}