using UnityEngine;

public class BeaconVisual : MonoBehaviour
{
    [Header("Visual Components")]
    [SerializeField] private Renderer statusLight;
    [SerializeField] private BeaconParticleController particleController;
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
            {
                statusLight = light.GetComponent<Renderer>();
            }
            else
            {
                statusLight = GetComponent<Renderer>();
            }
        }

        // Find particle controller if not assigned
        if (particleController == null)
        {
            particleController = GetComponentInChildren<BeaconParticleController>();
        }

        // create emissive material
        if (statusLight != null)
        {
            // Try URP shader first, fallback to Built-in Standard shader
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader != null)
            {
                lightMaterial = new Material(shader);
                lightMaterial.EnableKeyword("_EMISSION");
                statusLight.material = lightMaterial;
            }
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

            // Set base color for both URP and Built-in
            lightMaterial.SetColor("_BaseColor", currentColor); // URP
            lightMaterial.SetColor("_Color", currentColor); // Built-in
        }

        // Slowly rotate the beacon for visual effect
        transform.Rotate(Vector3.up, 10f * Time.deltaTime);
    }

    public void SetStatus(string status)
    {
        // Set particle color based on status
        if (particleController != null)
        {
            particleController.SetStatusColor(status);
        }

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
            // Set base color (works for both URP and Built-in)
            lightMaterial.SetColor("_BaseColor", color); // URP
            lightMaterial.SetColor("_Color", color); // Built-in fallback

            // Set emission color
            lightMaterial.SetColor("_EmissionColor", color);
        }
    }
}
