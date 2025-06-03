using UnityEngine;

public class MarcadorMagnetico : MonoBehaviour
{
    public ParticleSystem particleSystemVar;
    public float pullStrength = 5f;

    private ParticleSystem.Particle[] particles;

    void LateUpdate()
    {
        if (particleSystemVar == null) return;

        if (particles == null || particles.Length < particleSystemVar.main.maxParticles)
            particles = new ParticleSystem.Particle[particleSystemVar.main.maxParticles];

        int count = particleSystemVar.GetParticles(particles);
        Vector3 magnetPos = transform.position;

        for (int i = 0; i < count; i++)
        {
            Vector3 toMagnet = magnetPos - particles[i].position;
            float distance = toMagnet.magnitude;

            if (distance < 0.3f)
            {
                Vector3 force = toMagnet.normalized * pullStrength * Time.deltaTime;
                particles[i].velocity += force;
            }
        }

        particleSystemVar.SetParticles(particles, count);
    }
}
