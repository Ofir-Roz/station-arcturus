using System;
using UnityEngine;

[Serializable]
public class BeaconData
{
    public string id;
    public float x;
    public float y;
    public float z;
    public string status;

    // convert to unity vector3 position
    public Vector3 GetPosition()
    {
        return new Vector3(x, y, z);
    }

    // get color based on status
    public Color GetStatusColor()
    {
        switch (status.ToLower())
        {
            case "active":
                return Color.green;
            case "damaged":
                return Color.yellow;
            case "offline":
                return Color.red;
            default:
                return Color.white;
        }
    }
}

// Container for multiple beacons
[Serializable]
public class BeaconResponse
{
    public BeaconData[] beacons;
    public int count;
    public float time;
}