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

    [Header("Atmosphere Settings")]
    public bool enableAtmosphere = true;
    public Color atmosphereColor = new Color(0.4f, 0.6f, 1f, 0.35f); // Bluish with slightly higher alpha for more opacity
    public float atmosphereThickness = 1.35f; // increase radius: 35% larger than planet to give more room for beacons
    public float atmosphereIntensity = 1.0f; // Increase glow intensity slightly for better visibility
    public float atmosphereFadepower = 2.0f; // Controls edge fade (higher = softer edges)

    private GameObject planetSphere;
    private GameObject atmosphereSphere;

    void Start()
    {
        CreatePlanet();
        if (enableAtmosphere)
        {
            CreateAtmosphere();
        }
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

    void CreateAtmosphere()
    {
        // Create atmosphere sphere - larger than planet for mist effect
        atmosphereSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        atmosphereSphere.name = "Atmosphere";
        atmosphereSphere.transform.parent = planetSphere.transform; // Child of planet
        atmosphereSphere.transform.localPosition = Vector3.zero;
        atmosphereSphere.transform.localScale = Vector3.one * atmosphereThickness; // Thicker atmosphere

        // Create custom shader material with Fresnel fade effect
        Renderer renderer = atmosphereSphere.GetComponent<Renderer>();
        
        // Create shader that fades at edges (Fresnel effect)
        Shader atmosphereShader = Shader.Find("Universal Render Pipeline/Lit");
        Material atmosphereMat = new Material(atmosphereShader);
        
        // Set transparent rendering mode with additive blending for soft glow
        atmosphereMat.SetFloat("_Surface", 1); // Transparent
        atmosphereMat.SetFloat("_Blend", 0); // Alpha blending
        atmosphereMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        atmosphereMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One); // Additive for glow
        atmosphereMat.SetFloat("_ZWrite", 0);
        atmosphereMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        atmosphereMat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        atmosphereMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        atmosphereMat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off); // Render both sides

        // Set base color with low alpha for subtle effect
        Color baseColor = new Color(
            atmosphereColor.r, 
            atmosphereColor.g, 
            atmosphereColor.b, 
            atmosphereColor.a * 0.9f // Keep more alpha for a slightly more opaque atmosphere
        );
        atmosphereMat.SetColor("_BaseColor", baseColor);
        atmosphereMat.SetColor("_Color", baseColor);

        // Strong emission for glow that fades naturally
        atmosphereMat.EnableKeyword("_EMISSION");
        Color emissionColor = new Color(
            atmosphereColor.r * atmosphereIntensity * 1.2f,
            atmosphereColor.g * atmosphereIntensity * 1.2f,
            atmosphereColor.b * atmosphereIntensity * 1.2f,
            1.0f
        );
        atmosphereMat.SetColor("_EmissionColor", emissionColor);

        // Smooth surface for better blending
        atmosphereMat.SetFloat("_Smoothness", 0.95f);
        atmosphereMat.SetFloat("_Metallic", 0f);
        atmosphereMat.SetFloat("_SpecularHighlights", 0f);
        atmosphereMat.SetFloat("_EnvironmentReflections", 0f);

        renderer.material = atmosphereMat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        // Remove collider - atmosphere shouldn't block anything
        Collider atmosphereCollider = atmosphereSphere.GetComponent<Collider>();
        if (atmosphereCollider != null)
        {
            Destroy(atmosphereCollider);
        }

        Debug.Log("Atmosphere layer created with soft fading bluish mist effect");
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