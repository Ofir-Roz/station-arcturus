using UnityEngine;

public class BeaconVisual : MonoBehaviour
{
    [SerializeField] private Renderer statusLight;
    [SerializeField] private BeaconParticleController particleController;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseIntensity = 0.5f;

    private Color currentColor = Color.white;
    private Material lightMaterial;
    private float pulseTimer = 0f;

    void Awake()
    {
        if (statusLight == null) statusLight = GetComponent<Renderer>();
        if (particleController == null) particleController = GetComponentInChildren<BeaconParticleController>();

        if (statusLight != null)
        {
            lightMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            lightMaterial.EnableKeyword("_EMISSION");
            statusLight.material = lightMaterial;
        }
    }

    void Update()
    {
        if (lightMaterial != null)
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            float pulse = (Mathf.Sin(pulseTimer) * pulseIntensity) + 1f;
            lightMaterial.SetColor("_EmissionColor", currentColor * pulse);
            lightMaterial.SetColor("_BaseColor", currentColor);
        }
        
        transform.Rotate(Vector3.up, 10f * Time.deltaTime, Space.Self);
    }

    public void SetStatus(string status)
    {
        particleController?.SetStatusColor(status);

        switch (status.ToLower())
        {
            case "active":
                SetColor(Color.green, 2f);
                break;
            case "damaged":
                SetColor(Color.yellow, 4f);
                break;
            case "offline":
                SetColor(Color.red, 0.5f);
                break;
            default:
                SetColor(Color.white, 2f);
                break;
        }
    }

    void SetColor(Color color, float speed)
    {
        currentColor = color;
        pulseSpeed = speed;
        if (lightMaterial != null)
        {
            lightMaterial.SetColor("_BaseColor", color);
            lightMaterial.SetColor("_EmissionColor", color);
        }
    }
}
