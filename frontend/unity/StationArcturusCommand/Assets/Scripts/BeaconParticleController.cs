using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class BeaconParticleController : MonoBehaviour
{
    private ParticleSystem particles;
    private ParticleSystem.MainModule mainModule;
    
    void Awake()
    {
        particles = GetComponent<ParticleSystem>();
        mainModule = particles.main;
    }
    
    public void SetStatusColor(string status)
    {
        Color particleColor = Color.white;
        float emissionRate = 5f;
        
        switch(status.ToLower())
        {
            case "active":
                particleColor = new Color(0, 1, 0, 0.6f); // Green with transparency
                emissionRate = 5f;
                break;
            case "damaged":
                particleColor = new Color(1, 1, 0, 0.6f); // Yellow with transparency
                emissionRate = 10f; // More particles for damaged state
                break;
            case "offline":
                particleColor = new Color(1, 0, 0, 0.6f); // Red with transparency
                emissionRate = 2f; // Fewer particles for offline
                break;
        }
        
        // Apply color
        mainModule.startColor = particleColor;
        
        // Apply emission rate
        var emission = particles.emission;
        emission.rateOverTime = emissionRate;
    }
    
    public void StopEmission()
    {
        particles.Stop();
    }
    
    public void StartEmission()
    {
        particles.Play();
    }
}