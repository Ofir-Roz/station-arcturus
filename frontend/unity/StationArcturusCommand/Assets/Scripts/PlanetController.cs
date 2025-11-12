using UnityEngine;

public class PlanetController : MonoBehaviour
{
    [Header("Planet Settings")]
    public float planetRadius = 30f;
    public Material planetMaterial;
    public float rotationSpeed = 2f; // Degrees per second

    [Header("Visual Settings")]
    public Color planetColor = new Color(0.2f, 0.3f, 0.5f);
    public float atmosphereGlow = 0.5f;

    private GameObject planetSphere;

    void Start()
    {
        CreatePlanet();
    }

    void CreatePlanet()
    {
        // Create sphere GameObject
        planetSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        planetSphere.name = "Planet_Arcturus";
        planetSphere.transform.parent = transform;
        planetSphere.transform.localPosition = Vector3.zero;
        planetSphere.transform.localScale = Vector3.one * planetRadius * 2; // Diameter

        // Setup material
        Renderer renderer = planetSphere.GetComponent<Renderer>();
        if (planetMaterial != null)
        {
            renderer.material = planetMaterial;
        }
        else
        {
            // Create default material
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = planetColor;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", planetColor * atmosphereGlow);
            renderer.material = mat;
        }

        // Remove collider if not needed for physics
        Collider collider = planetSphere.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }

    void Update()
    {
        // Slow rotation for visual interest
        if (planetSphere != null)
        {
            planetSphere.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }

    public float GetRadius()
    {
        return planetRadius;
    }
}