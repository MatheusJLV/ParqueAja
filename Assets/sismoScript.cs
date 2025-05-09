using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class sismoScript : MonoBehaviour
{
    public GameObject plataforma; // Referencia a la plataforma

    public float speed = 2f; // Velocidad del movimiento
    public float range = 2f; // Rango del movimiento
    public float duracion = 5f; // Duraci�n en segundos
    public float multiplier = 0.01f; // Duraci�n en segundos

    private Coroutine currentMovement; // Corrutina actual en ejecuci�n

    // Botones para iniciar los movimientos
    public Button iniciarLadoBtn; // Bot�n para iniciar el movimiento de lado a lado
    public Button iniciarFrenteBtn; // Bot�n para iniciar el movimiento de adelante hacia atr�s
    public Button iniciarArribaBtn; // Bot�n para iniciar el movimiento de arriba hacia abajo

    // Sliders para controlar velocidad y rango
    public Slider velocidadSld; // Slider para controlar la velocidad
    public Slider rangoSld; // Slider para controlar el rango
    public Slider duracionSd; // Slider para controlar la duracion

    public GameObject jugadorRig; // GameObject que representa al jugador

    // Botones para teletransportaci�n
    public Button entradaBtn; // Bot�n para teletransportarse a la plataforma
    public Button salidaBtn; // Bot�n para teletransportarse al suelo

    // Variables para teletransportaci�n
    public TeleportationAnchor plataformaTP; // TeleportationAnchor para el asiento
    public TeleportationAnchor sueloTP; // TeleportationAnchor para el suelo

    // Nuevas variables GameObject
    public GameObject puertaGO; // Referencia a la puerta
    public GameObject escalonGO; // Referencia al escal�n
    public GameObject escalonesGO; // Referencia a los escalones

    // Nuevos botones para abrir y cerrar
    public Button abrirBtn; // Bot�n para abrir
    public Button cerrarBtn; // Bot�n para cerrar

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
        if (duracionSd != null)
        {
            duracionSd.onValueChanged.AddListener(OnDuracionSliderChanged);
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
        // Suscribirse a los eventos de los botones abrir y cerrar
        if (abrirBtn != null)
        {
            abrirBtn.onClick.AddListener(Abrir);
        }

        if (cerrarBtn != null)
        {
            cerrarBtn.onClick.AddListener(Cerrar);
        }
        // Mover la puerta hacia abajo al iniciar
        StartCoroutine(MoverPuertaHaciaAbajo());
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

        while (elapsedTime < duracion)
        {
            float offset = Mathf.Sin(elapsedTime * speed) * range;
            plataforma.transform.localPosition = startPosition + multiplier * new Vector3(offset, 0f, 0f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        //plataforma.transform.localPosition = startPosition; // Regresar a la posici�n inicial
        ReturnToOrigin();
    }

    // Corrutina para mover de adelante hacia atr�s
    private IEnumerator MoveFrontToBack()
    {
        Vector3 startPosition = plataforma.transform.localPosition;
        float elapsedTime = 0f;

        while (elapsedTime < duracion)
        {
            float offset = Mathf.Sin(elapsedTime * speed) * range;
            plataforma.transform.localPosition = startPosition + multiplier * new Vector3(0f, 0f, offset);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        //plataforma.transform.localPosition = startPosition; // Regresar a la posici�n inicial
        ReturnToOrigin();
    }

    // Corrutina para mover de arriba hacia abajo
    private IEnumerator MoveUpAndDown()
    {
        Vector3 startPosition = plataforma.transform.localPosition;
        float elapsedTime = 0f;

        while (elapsedTime < duracion)
        {
            float offset = Mathf.Abs(Mathf.Sin(elapsedTime * speed)) * range;
            plataforma.transform.localPosition = startPosition + multiplier * new Vector3(0f, offset, 0f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        //plataforma.transform.localPosition = startPosition; // Regresar a la posici�n inicial
        ReturnToOrigin();
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
        if (value>2)
        {
            multiplier= 1f;
        }
        else
        {
            multiplier= 0.01f;
        }
    }

    private void OnDuracionSliderChanged(float value)
    {
        duracion = value;
        Debug.Log($"Duraci�n actualizada a: {duracion}");
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
    // Funci�n para abrir (animar la puerta hacia abajo y los escalones hacia arriba)
    public void Abrir()
    {
        StartCoroutine(AnimarPuertaEscalones(1.5f*Vector3.back, Vector3.forward));
    }

    // Funci�n para cerrar (animar la puerta hacia arriba y los escalones hacia abajo)
    public void Cerrar()
    {
        StartCoroutine(AnimarPuertaEscalones(1.5f * Vector3.forward, Vector3.back));
    }

    // Corrutina para animar la puerta y los escalones
    private IEnumerator AnimarPuertaEscalones(Vector3 puertaMovimiento, Vector3 escalonesMovimiento)
    {
        float duracion = 1f; // Duraci�n de la animaci�n en segundos
        float tiempoTranscurrido = 0f;

        // Posiciones iniciales
        Vector3 puertaPosInicial = puertaGO.transform.localPosition;
        Vector3 escalonPosInicial = escalonGO.transform.localPosition;
        Vector3 escalonesPosInicial = escalonesGO.transform.localPosition;

        // Posiciones finales
        Vector3 puertaPosFinal = puertaPosInicial + puertaMovimiento;
        Vector3 escalonPosFinal = escalonPosInicial + escalonesMovimiento;
        Vector3 escalonesPosFinal = escalonesPosInicial + escalonesMovimiento;

        while (tiempoTranscurrido < duracion)
        {
            float t = tiempoTranscurrido / duracion;

            // Interpolaci�n lineal para mover los objetos
            puertaGO.transform.localPosition = Vector3.Lerp(puertaPosInicial, puertaPosFinal, t);
            escalonGO.transform.localPosition = Vector3.Lerp(escalonPosInicial, escalonPosFinal, t);
            escalonesGO.transform.localPosition = Vector3.Lerp(escalonesPosInicial, escalonesPosFinal, t);

            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        // Asegurarse de que los objetos lleguen exactamente a su posici�n final
        puertaGO.transform.localPosition = puertaPosFinal;
        escalonGO.transform.localPosition = escalonPosFinal;
        escalonesGO.transform.localPosition = escalonesPosFinal;
    }
    private IEnumerator MoverPuertaHaciaAbajo()
    {
        float duracion = 1f; // Duraci�n de la animaci�n en segundos
        float tiempoTranscurrido = 0f;

        // Posici�n inicial y final de la puerta
        Vector3 puertaPosInicial = puertaGO.transform.localPosition;
        Vector3 puertaPosFinal = puertaPosInicial + 1.5f * Vector3.back;

        while (tiempoTranscurrido < duracion)
        {
            float t = tiempoTranscurrido / duracion;

            // Interpolaci�n lineal para mover la puerta
            puertaGO.transform.localPosition = Vector3.Lerp(puertaPosInicial, puertaPosFinal, t);

            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        // Asegurarse de que la puerta llegue exactamente a su posici�n final
        puertaGO.transform.localPosition = puertaPosFinal;
    }
}
