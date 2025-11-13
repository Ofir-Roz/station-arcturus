using UnityEngine;

public class PlanetController : MonoBehaviour
{
    [Header("Planet Settings")]
    public float planetRadius = 30f;
    public Material planetMaterial;
    public float rotationSpeed = 2f;
    public Color planetColor = new Color(0.2f, 0.3f, 0.5f);

    [Header("Atmosphere")]
    public bool enableAtmosphere = true;
    public Color atmosphereColor = new Color(0.4f, 0.6f, 1f, 0.35f);
    public float atmosphereThickness = 1.35f;

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
        planetSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        planetSphere.name = "Planet_Arcturus";
        planetSphere.transform.SetParent(transform);
        planetSphere.transform.localPosition = Vector3.zero;
        planetSphere.transform.localScale = Vector3.one * planetRadius * 2;

        Renderer renderer = planetSphere.GetComponent<Renderer>();
        if (planetMaterial != null)
        {
            renderer.material = planetMaterial;
        }
        else
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = planetColor;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", planetColor * 0.5f);
            renderer.material = mat;
        }
        Destroy(planetSphere.GetComponent<Collider>());
    }

    void CreateAtmosphere()
    {
        atmosphereSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        atmosphereSphere.name = "Atmosphere";
        atmosphereSphere.transform.SetParent(planetSphere.transform);
        atmosphereSphere.transform.localPosition = Vector3.zero;
        atmosphereSphere.transform.localScale = Vector3.one * atmosphereThickness;

        Renderer renderer = atmosphereSphere.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        
        // Transparent rendering with additive blending for glow
        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_Blend", 0);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        mat.SetFloat("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        
        // Base color with alpha
        Color baseColor = new Color(atmosphereColor.r, atmosphereColor.g, atmosphereColor.b, atmosphereColor.a * 0.9f);
        mat.SetColor("_BaseColor", baseColor);
        
        // Emission for glow
        mat.EnableKeyword("_EMISSION");
        Color emissionColor = new Color(atmosphereColor.r * 1.2f, atmosphereColor.g * 1.2f, atmosphereColor.b * 1.2f, 1f);
        mat.SetColor("_EmissionColor", emissionColor);
        
        // Smooth surface for blending
        mat.SetFloat("_Smoothness", 0.95f);
        
        renderer.material = mat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        
        Destroy(atmosphereSphere.GetComponent<Collider>());
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