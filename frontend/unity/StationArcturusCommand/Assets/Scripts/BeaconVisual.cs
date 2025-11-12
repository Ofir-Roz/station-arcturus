using UnityEngine;

public class BeaconVisual : MonoBehaviour
{
    [Header("Beacon Components")]
    [SerializeField] private Renderer statusLight;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseIntensity = 0.5f;

    private Color currentColor = Color.white;
    private Material lightMaterial;
    private float pulseTimer = 0f;

    void Awake()
    {
        // Find status light if not assigned
        if (statusLight == null)
        {
            Transform light = transform.Find("StatusLight");
            if (light != null)
                statusLight = light.GetComponent<Renderer>();
        }

        // create emissive material
        if (statusLight != null)
        {
            lightMaterial = new Material(Shader.Find("Standard"));
            lightMaterial.EnableKeyword("_EMISSION");
            statusLight.material = lightMaterial;
        }
    }

    void Update()
    {
        // Animate status light with pulsing effect
        if (lightMaterial != null)
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            float pulse = (Mathf.Sin(pulseTimer) * pulseIntensity) + 1f;

            Color emissiveColor = currentColor * pulse;
            lightMaterial.SetColor("_EmissionColor", emissiveColor);
            lightMaterial.color = currentColor;
        }

        // Slowly rotate the beacon for visual effect
        transform.Rotate(Vector3.up, 10f * Time.deltaTime);
    }

    public void SetStatus(string status)
    {
        switch (status.ToLower())
        {
            case "active":
                SetColor(Color.green);
                pulseSpeed = 2f;
                break;
            case "damaged":
                SetColor(Color.yellow);
                pulseSpeed = 4f;
                break;
            case "offline":
                SetColor(Color.red);
                pulseSpeed = 0.5f;
                break;
            default:
                SetColor(Color.white);
                break;
        }
    }

    private void SetColor(Color color)
    {
        currentColor = color;
        if (lightMaterial != null)
        {
            lightMaterial.color = color;
        }
    }
}
