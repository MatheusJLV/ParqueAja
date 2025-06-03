using System.Collections.Generic;
using UnityEngine;

/*
 * El script causo mucho caos, no apto para dibujo magnetico pero tal vez si para puntillada
 * si se cambia el eje del movimiento magnetico perpendicularmente. conservar por ahora
 */

public class ParticleManager : MonoBehaviour
{
    public GameObject particlePrefab;
    public Transform magnet; // Your magnetic marker
    public Transform attractionPlane; // The drawing surface (particles stick here)

    public int gridX = 40;
    public int gridY = 40;
    public float spacing = 0.025f;
    public float attractionRadius = 0.15f;
    public float moveSpeed = 5f;

    private List<Transform> particles = new List<Transform>();

    void Start()
    {
        GenerateParticles();
    }

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
