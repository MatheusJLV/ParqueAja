using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class sismoScript : MonoBehaviour
{
    public GameObject plataforma; // Referencia a la plataforma

    public float speed = 2f; // Velocidad del movimiento
    public float range = 2f; // Rango del movimiento

    private Coroutine currentMovement; // Corrutina actual en ejecuci�n

    // Botones para iniciar los movimientos
    public Button iniciarLadoBtn; // Bot�n para iniciar el movimiento de lado a lado
    public Button iniciarFrenteBtn; // Bot�n para iniciar el movimiento de adelante hacia atr�s
    public Button iniciarArribaBtn; // Bot�n para iniciar el movimiento de arriba hacia abajo

    // Sliders para controlar velocidad y rango
    public Slider velocidadSld; // Slider para controlar la velocidad
    public Slider rangoSld; // Slider para controlar el rango

    public GameObject jugadorRig; // GameObject que representa al jugador

    // Botones para teletransportaci�n
    public Button entradaBtn; // Bot�n para teletransportarse a la plataforma
    public Button salidaBtn; // Bot�n para teletransportarse al suelo

    // Variables para teletransportaci�n
    public TeleportationAnchor plataformaTP; // TeleportationAnchor para el asiento
    public TeleportationAnchor sueloTP; // TeleportationAnchor para el suelo

    void Start()
    {
        // Suscribirse a los eventos de los botones de movimiento
        if (iniciarLadoBtn != null)
        {
            iniciarLadoBtn.onClick.AddListener(StartSideToSide);
        }

        if (iniciarFrenteBtn != null)
        {
            iniciarFrenteBtn.onClick.AddListener(StartFrontToBack);
        }

        if (iniciarArribaBtn != null)
        {
            iniciarArribaBtn.onClick.AddListener(StartUpAndDown);
        }

        // Suscribirse a los eventos de los sliders
        if (velocidadSld != null)
        {
            velocidadSld.onValueChanged.AddListener(OnVelocidadSliderChanged);
        }

        if (rangoSld != null)
        {
            rangoSld.onValueChanged.AddListener(OnRangoSliderChanged);
        }

        // Suscribirse a los eventos de los botones de teletransportaci�n
        if (entradaBtn != null)
        {
            entradaBtn.onClick.AddListener(OnEntradaButtonClicked);
        }

        if (salidaBtn != null)
        {
            salidaBtn.onClick.AddListener(OnSalidaButtonClicked);
        }
    }

    // M�todo para iniciar el movimiento de lado a lado
    public void StartSideToSide()
    {
        StopCurrentMovement(); // Detener cualquier movimiento en curso
        currentMovement = StartCoroutine(MoveSideToSide());
    }

    // M�todo para iniciar el movimiento de adelante hacia atr�s
    public void StartFrontToBack()
    {
        StopCurrentMovement(); // Detener cualquier movimiento en curso
        currentMovement = StartCoroutine(MoveFrontToBack());
    }

    // M�todo para iniciar el movimiento de arriba hacia abajo
    public void StartUpAndDown()
    {
        StopCurrentMovement(); // Detener cualquier movimiento en curso
        currentMovement = StartCoroutine(MoveUpAndDown());
    }

    // M�todo para detener el movimiento actual y regresar al origen
    public void StopCurrentMovement()
    {
        if (currentMovement != null)
        {
            StopCoroutine(currentMovement);
            currentMovement = null;
        }

        // Iniciar la corrutina para regresar al origen
        currentMovement = StartCoroutine(ReturnToOrigin());
    }

    // Corrutina para mover de lado a lado
    private IEnumerator MoveSideToSide()
    {
        Vector3 startPosition = plataforma.transform.localPosition;
        float elapsedTime = 0f;

        while (true)
        {
            float offset = Mathf.Sin(elapsedTime * speed) * range;
            plataforma.transform.localPosition = startPosition + new Vector3(offset, 0f, 0f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    // Corrutina para mover de adelante hacia atr�s
    private IEnumerator MoveFrontToBack()
    {
        Vector3 startPosition = plataforma.transform.localPosition;
        float elapsedTime = 0f;

        while (true)
        {
            float offset = Mathf.Sin(elapsedTime * speed) * range;
            plataforma.transform.localPosition = startPosition + new Vector3(0f, 0f, offset);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    // Corrutina para mover de arriba hacia abajo
    private IEnumerator MoveUpAndDown()
    {
        Vector3 startPosition = plataforma.transform.localPosition;
        float elapsedTime = 0f;

        while (true)
        {
            float offset = Mathf.Sin(elapsedTime * speed) * range;
            plataforma.transform.localPosition = startPosition + new Vector3(0f, offset, 0f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    // Corrutina para regresar al origen local (0, 0, 0)
    private IEnumerator ReturnToOrigin()
    {
        Vector3 startPosition = plataforma.transform.localPosition;
        Vector3 targetPosition = Vector3.zero; // Origen local
        float elapsedTime = 0f;

        while (elapsedTime < 1f) // Duraci�n de 1 segundo para regresar al origen
        {
            plataforma.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, elapsedTime);
            elapsedTime += Time.deltaTime * speed; // Ajustar la velocidad de retorno
            yield return null;
        }

        // Asegurarse de que la posici�n final sea exactamente el origen
        plataforma.transform.localPosition = targetPosition;
    }

    // M�todo para manejar el cambio de valor del slider de velocidad
    private void OnVelocidadSliderChanged(float value)
    {
        speed = value;
        Debug.Log($"Velocidad actualizada a: {speed}");
    }

    // M�todo para manejar el cambio de valor del slider de rango
    private void OnRangoSliderChanged(float value)
    {
        range = value;
        Debug.Log($"Rango actualizado a: {range}");
    }

    private void OnEntradaButtonClicked()
    {
        if (plataformaTP != null)
        {
            plataformaTP.RequestTeleport();
            Debug.Log("Jugador teletransportado a la plataforma.");
        }
        else
        {
            Debug.LogWarning("El TeleportationAnchor 'plataformaTP' no est� asignado.");
        }

        if (jugadorRig != null)
        {
            jugadorRig.transform.SetParent(plataforma.transform);
            Debug.Log("Jugador ahora es hijo de la plataforma.");
        }
        else
        {
            Debug.LogWarning("El jugadorRig no est� asignado.");
        }
    }

    private void OnSalidaButtonClicked()
    {
        if (sueloTP != null)
        {
            sueloTP.RequestTeleport();
            Debug.Log("Jugador teletransportado al suelo.");
        }
        else
        {
            Debug.LogWarning("El TeleportationAnchor 'sueloTP' no est� asignado.");
        }

        if (jugadorRig != null)
        {
            jugadorRig.transform.SetParent(null);
            Debug.Log("Jugador liberado de la plataforma.");
        }
        else
        {
            Debug.LogWarning("El jugadorRig no est� asignado.");
        }
    }

    void OnDestroy()
    {
        // Desuscribirse de los eventos de los botones de movimiento
        if (iniciarLadoBtn != null)
        {
            iniciarLadoBtn.onClick.RemoveListener(StartSideToSide);
        }

        if (iniciarFrenteBtn != null)
        {
            iniciarFrenteBtn.onClick.RemoveListener(StartFrontToBack);
        }

        if (iniciarArribaBtn != null)
        {
            iniciarArribaBtn.onClick.RemoveListener(StartUpAndDown);
        }

        // Desuscribirse de los eventos de los sliders
        if (velocidadSld != null)
        {
            velocidadSld.onValueChanged.RemoveListener(OnVelocidadSliderChanged);
        }

        if (rangoSld != null)
        {
            rangoSld.onValueChanged.RemoveListener(OnRangoSliderChanged);
        }

        // Desuscribirse de los eventos de los botones de teletransportaci�n
        if (entradaBtn != null)
        {
            entradaBtn.onClick.RemoveListener(OnEntradaButtonClicked);
        }

        if (salidaBtn != null)
        {
            salidaBtn.onClick.RemoveListener(OnSalidaButtonClicked);
        }
    }
}
