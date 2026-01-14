using System.Collections.Generic;
using UnityEngine;

/*
 * El script causó mucho caos, no apto para dibujo magnético pero tal vez sí para puntillada
 * si se cambia el eje del movimiento magnético perpendicularmente. conservar por ahora
 */

// Generador de partículas en cuadrícula: crea una malla de partículas atraídas hacia un imán y un plano.
public class ParticleManager : MonoBehaviour
{
    public GameObject particlePrefab;        // Prefab de partícula a instanciar
    public Transform magnet;                 // Marcador magnético que atrae partículas
    public Transform attractionPlane;        // Superficie de dibujo donde se adhieren las partículas

    public int gridX = 40;                   // Número de columnas de la cuadrícula
    public int gridY = 40;                   // Número de filas de la cuadrícula
    public float spacing = 0.025f;           // Distancia entre partículas
    public float attractionRadius = 0.15f;   // Radio de acción magnética
    public float moveSpeed = 5f;             // Velocidad de movimiento de partículas

    private List<Transform> particles = new List<Transform>();

    // Genera la cuadrícula inicial de partículas en el Start.
    void Start()
    {
        GenerateParticles();
    }

    // Cada frame: atrae partículas hacia el imán y las proyecta al plano.
    void Update()
    {
        foreach (var particle in particles)
        {
            Vector3 targetPoint = ProjectToPlane(particle.position);
            float dist = Vector3.Distance(particle.position, magnet.position);

            if (dist < attractionRadius)
            {
                Vector3 toMagnet = magnet.position - particle.position;
                targetPoint += toMagnet.normalized * 0.01f; // small pull
            }

            particle.position = Vector3.Lerp(particle.position, targetPoint, Time.deltaTime * moveSpeed);
        }
    }

    // Instancia un arreglo de partículas en cuadrícula sobre el plano de atracción.
    void GenerateParticles()
    {
        Vector3 origin = attractionPlane.position;
        Vector3 right = attractionPlane.right;
        Vector3 up = attractionPlane.up;

        for (int x = 0; x < gridX; x++)
        {
            for (int y = 0; y < gridY; y++)
            {
                Vector3 offset = right * (x * spacing) + up * (y * spacing);
                Vector3 pos = origin + offset;
                GameObject particle = Instantiate(particlePrefab, pos, Quaternion.identity, this.transform);
                particles.Add(particle.transform);
            }
        }
    }

    // Proyecta una posición de partícula al plano de atracción.
    Vector3 ProjectToPlane(Vector3 particlePos)
    {
        // Project toward attractionPlane, constrained to its forward direction
        Vector3 planePos = attractionPlane.position;
        Vector3 planeNormal = attractionPlane.forward;

        Vector3 toPlane = planePos - particlePos;
        float projection = Vector3.Dot(toPlane, planeNormal);
        return particlePos + planeNormal * projection;
    }
}
