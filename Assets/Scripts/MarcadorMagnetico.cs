using UnityEngine;

// Marcador magnético: atrae partículas hacia su posición y reproduce un sonido al impactar.
public class MarcadorMagnetico : MonoBehaviour
{
    [Header("Magnetic Behavior")]
    public ParticleSystem particleSystemVar; // Sistema de partículas a magnetizar
    public float pullStrength = 5f;          // Intensidad de la fuerza de atracción

    [Header("Audio")]
    [SerializeField] private AudioSource impactAS; // Sonido al detectar entrada en el trigger

    private ParticleSystem.Particle[] particles;

    // Aplica atracción magnética a las partículas cada frame tardío.
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
                // Calcula fuerza proporcional al pullStrength y la suma a la velocidad
                Vector3 force = toMagnet.normalized * pullStrength * Time.deltaTime;
                particles[i].velocity += force;
            }
        }

        particleSystemVar.SetParticles(particles, count);
    }

    // Opcional: reproduce sonido cuando algo entra en el trigger magnético.
    private void OnTriggerEnter(Collider other)
    {
        if (impactAS != null && !impactAS.isPlaying)
        {
            impactAS.Play();
        }
    }
}
