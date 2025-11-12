using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class UIController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI connectionStatusText;
    [SerializeField] private TextMeshProUGUI totalBeaconsText;
    [SerializeField] private TextMeshProUGUI activeBeaconsText;
    [SerializeField] private TextMeshProUGUI damagedBeaconsText;
    [SerializeField] private TextMeshProUGUI offlineBeaconsText;
    [SerializeField] private Image connectionIndicator;

    [Header("Planetary Display")]
    [SerializeField] private TextMeshProUGUI planetNameText;
    [SerializeField] private TextMeshProUGUI planetRadiusText;

    [Header("References")]
    [SerializeField] private BeaconManager beaconManager;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.5f;

    private float lastUpdateTime = 0f;
    private bool wasConnected = false;

    void Start()
    {
        if (beaconManager == null)
        {
            beaconManager = FindObjectOfType<BeaconManager>();
        }

        UpdateUI();
    }

    void Update()
    {
        // Update UI periodically
        if (Time.time - lastUpdateTime > updateInterval)
        {
            UpdateUI();
            lastUpdateTime = Time.time;
        }
    }

    void UpdateUI()
    {
        if (beaconManager == null) return;

        // Update connection status
        bool isConnected = beaconManager.isConnected;

        if (connectionStatusText != null)
        {
            connectionStatusText.text = isConnected ? "CONNECTED" : "DISCONNECTED";
            connectionStatusText.color = isConnected ? Color.green : Color.red;
        }

        if (connectionIndicator != null)
        {
            connectionIndicator.color = isConnected ? Color.green : Color.red;
        }

        // Flash on connection change
        if (isConnected != wasConnected)
        {
            wasConnected = isConnected;
            if (isConnected)
            {
                StartCoroutine(FlashText(connectionStatusText, Color.white, 0.5f));
            }
        }

        // Update beacon statistics
        Dictionary<string, int> stats = beaconManager.GetBeaconStats();

        if (totalBeaconsText != null)
            totalBeaconsText.text = $"Total Beacons: {stats["total"]}";

        if (activeBeaconsText != null)
        {
            activeBeaconsText.text = $"Active: {stats["active"]}";
            activeBeaconsText.color = Color.green;
        }

        if (damagedBeaconsText != null)
        {
            damagedBeaconsText.text = $"Damaged: {stats["damaged"]}";
            damagedBeaconsText.color = Color.yellow;
        }

        if (offlineBeaconsText != null)
        {
            offlineBeaconsText.text = $"Offline: {stats["offline"]}";
            offlineBeaconsText.color = Color.red;
        }

        // Update planetary info
        if (planetNameText != null)
        {
            planetNameText.text = "Planet: Arcturus Prime";
        }

        if (planetRadiusText != null && beaconManager.planetController != null)
        {
            float radius = beaconManager.planetController.GetRadius();
            planetRadiusText.text = $"Radius: {radius:F1} units";
        }
    }

    IEnumerator FlashText(TextMeshProUGUI text, Color flashColor, float duration)
    {
        if (text == null) yield break;

        Color originalColor = text.color;
        text.color = flashColor;
        yield return new WaitForSeconds(duration);
        text.color = originalColor;
    }
}