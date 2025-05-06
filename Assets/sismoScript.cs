using System.Collections;
using UnityEngine;

public class sismoScript : MonoBehaviour
{
    public GameObject plataforma; // Referencia a la plataforma

    public float speed = 2f; // Velocidad del movimiento
    public float range = 2f; // Rango del movimiento

    private Coroutine currentMovement; // Corrutina actual en ejecución

    // Método para iniciar el movimiento de lado a lado
    public void StartSideToSide()
    {
        StopCurrentMovement(); // Detener cualquier movimiento en curso
        currentMovement = StartCoroutine(MoveSideToSide());
    }

    // Método para iniciar el movimiento de adelante hacia atrás
    public void StartFrontToBack()
    {
        StopCurrentMovement(); // Detener cualquier movimiento en curso
        currentMovement = StartCoroutine(MoveFrontToBack());
    }

    // Método para iniciar el movimiento de arriba hacia abajo
    public void StartUpAndDown()
    {
        StopCurrentMovement(); // Detener cualquier movimiento en curso
        currentMovement = StartCoroutine(MoveUpAndDown());
    }

    // Método para detener el movimiento actual
    public void StopCurrentMovement()
    {
        if (currentMovement != null)
        {
            StopCoroutine(currentMovement);
            currentMovement = null;
        }
    }

    // Corrutina para mover de lado a lado
    private IEnumerator MoveSideToSide()
    {
        Vector3 startPosition = plataforma.transform.position;
        float elapsedTime = 0f;

        while (true)
        {
            float offset = Mathf.Sin(elapsedTime * speed) * range;
            plataforma.transform.position = startPosition + new Vector3(offset, 0f, 0f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    // Corrutina para mover de adelante hacia atrás
    private IEnumerator MoveFrontToBack()
    {
        Vector3 startPosition = plataforma.transform.position;
        float elapsedTime = 0f;

        while (true)
        {
            float offset = Mathf.Sin(elapsedTime * speed) * range;
            plataforma.transform.position = startPosition + new Vector3(0f, 0f, offset);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    // Corrutina para mover de arriba hacia abajo
    private IEnumerator MoveUpAndDown()
    {
        Vector3 startPosition = plataforma.transform.position;
        float elapsedTime = 0f;

        while (true)
        {
            float offset = Mathf.Sin(elapsedTime * speed) * range;
            plataforma.transform.position = startPosition + new Vector3(0f, offset, 0f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
}
