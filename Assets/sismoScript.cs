using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class sismoScript : MonoBehaviour
{
    public GameObject plataforma; // Referencia a la plataforma

    private bool abierto = true; // Estado de la puerta (abierta o cerrada)

    public float speed = 2f; // Velocidad del movimiento
    public float range = 2f; // Rango del movimiento
    public float duracion = 5f; // Duración en segundos
    public float multiplier = 0.01f; // Duración en segundos

    private Coroutine currentMovement; // Corrutina actual en ejecución

    // Botones para iniciar los movimientos
    public Button iniciarLadoBtn; // Botón para iniciar el movimiento de lado a lado
    public Button iniciarFrenteBtn; // Botón para iniciar el movimiento de adelante hacia atrás
    public Button iniciarArribaBtn; // Botón para iniciar el movimiento de arriba hacia abajo

    // Sliders para controlar velocidad y rango
    public Slider velocidadSld; // Slider para controlar la velocidad
    public Slider rangoSld; // Slider para controlar el rango
    public Slider duracionSd; // Slider para controlar la duracion

    public GameObject jugadorRig; // GameObject que representa al jugador

    // Botones para teletransportación
    public Button entradaBtn; // Botón para teletransportarse a la plataforma
    public Button salidaBtn; // Botón para teletransportarse al suelo

    // Variables para teletransportación
    public TeleportationAnchor plataformaTP; // TeleportationAnchor para el asiento
    public TeleportationAnchor sueloTP; // TeleportationAnchor para el suelo

    // Nuevas variables GameObject
    public GameObject puertaGO; // Referencia a la puerta
    public GameObject escalonGO; // Referencia al escalón
    public GameObject escalonesGO; // Referencia a los escalones

    // Nuevos botones para abrir y cerrar
    public Button abrirBtn; // Botón para abrir
    public Button cerrarBtn; // Botón para cerrar

    private Vector3 originalPosition;
    float duracionDesfase = 3f; // Duración de la elevación


    void Start()
    {
        originalPosition = plataforma.transform.localPosition;

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

        // Suscribirse a los eventos de los botones de teletransportación
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

    // Método para detener el movimiento actual y regresar al origen
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
        if (multiplier != 0.01f)
        {
            yield return StartCoroutine(ElevarPlataforma());
        }
        Vector3 startPosition = plataforma.transform.localPosition;
        float elapsedTime = 0f;

        while (elapsedTime < duracion)
        {
            float offset = Mathf.Sin(elapsedTime * speed) * range;
            plataforma.transform.localPosition = startPosition + multiplier * new Vector3(offset, 0f, 0f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (multiplier != 0.01f)
        {
            yield return StartCoroutine(BajarPlataforma());
        }

        //plataforma.transform.localPosition = startPosition; // Regresar a la posición inicial
        yield return StartCoroutine(ReturnToOrigin());

    }

    // Corrutina para mover de adelante hacia atrás
    private IEnumerator MoveFrontToBack()
    {
        if (multiplier != 0.01f)
        {
            yield return StartCoroutine(ElevarPlataforma());
        }
        Vector3 startPosition = plataforma.transform.localPosition;
        float elapsedTime = 0f;

        while (elapsedTime < duracion)
        {
            float offset = Mathf.Sin(elapsedTime * speed) * range;
            plataforma.transform.localPosition = startPosition + multiplier * new Vector3(0f, offset, 0f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (multiplier != 0.01f)
        {
            yield return StartCoroutine(BajarPlataforma());
        }

        //plataforma.transform.localPosition = startPosition; // Regresar a la posición inicial
        yield return StartCoroutine(ReturnToOrigin());

    }

    // Corrutina para mover de arriba hacia abajo
    private IEnumerator MoveUpAndDown()
    {
        if (multiplier != 0.01f)
        {
            yield return StartCoroutine(ElevarPlataforma());
        }

        Vector3 startPosition = plataforma.transform.localPosition;
        float elapsedTime = 0f;

        while (elapsedTime < duracion)
        {
            float offset = Mathf.Abs(Mathf.Sin(elapsedTime * speed)) * range;
            plataforma.transform.localPosition = startPosition + multiplier * new Vector3(0f, 0f, offset);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (multiplier != 0.01f)
        {
            yield return StartCoroutine(BajarPlataforma());
        }

        //plataforma.transform.localPosition = startPosition; // Regresar a la posición inicial
        yield return StartCoroutine(ReturnToOrigin());

    }

    private IEnumerator ElevarPlataforma(float altura=2f)
    {
        Vector3 startPosition = plataforma.transform.localPosition;
        Vector3 targetPosition = startPosition + new Vector3(0f, 0f, altura);
        float elapsedTime = 0f;
        //float duracionElevacion = 1f; // Duración de la elevación

        while (elapsedTime < duracionDesfase)
        {
            plataforma.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duracionDesfase);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        plataforma.transform.localPosition = targetPosition;
    }

    private IEnumerator BajarPlataforma()
    {
        Vector3 startPosition = plataforma.transform.localPosition;
        Vector3 targetPosition = new Vector3(startPosition.x, startPosition.y, 0f); // Asume que la posición original es y=0
        float elapsedTime = 0f;
        //float duracionDescenso = 1f; // Duración del descenso

        while (elapsedTime < duracionDesfase)
        {
            plataforma.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duracionDesfase);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        plataforma.transform.localPosition = targetPosition;
    }


    

    // Corrutina para regresar al origen local (0, 0, 0)
    private IEnumerator ReturnToOrigin()
    {
        Vector3 targetPosition = originalPosition;

        Vector3 startPosition = plataforma.transform.localPosition;
        //Vector3 targetPosition = Vector3.zero;
        float elapsedTime = 0f;
        float duration = 1f; // puedes ajustarlo según necesidad

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            plataforma.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        plataforma.transform.localPosition = targetPosition;


        /*Vector3 startPosition = plataforma.transform.localPosition;
        Vector3 targetPosition = Vector3.zero; // Origen local
        float elapsedTime = 0f;

        while (elapsedTime < 1f) // Duración de 1 segundo para regresar al origen
        {
            plataforma.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, elapsedTime);
            elapsedTime += Time.deltaTime * speed; // Ajustar la velocidad de retorno
            yield return null;
        }

        // Asegurarse de que la posición final sea exactamente el origen
        plataforma.transform.localPosition = targetPosition;*/




        /*Vector3 startPosition = plataforma.transform.localPosition;
        //Vector3 targetPosition = Vector3.zero; // Origen local
        float tolerance = 0.01f; // Margen de error para considerar que se alcanzó el destino

        while (Vector3.Distance(plataforma.transform.localPosition, targetPosition) > tolerance)
        {
            plataforma.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, Time.deltaTime * speed);
            yield return null;
        }

        // Asegurarse de que la posición final sea exactamente el origen
        plataforma.transform.localPosition = targetPosition;*/





        // Retornar al origen en el plano horizontal
        //yield return StartCoroutine(ReturnToHorizontalOrigin());

        // Si es un recorrido extremo, también retornar al origen en el plano vertical
        /*if (multiplier != 0.01f)
        {
            yield return StartCoroutine(ReturnToVerticalOrigin());
        }*/
    }

    // Método para manejar el cambio de valor del slider de velocidad
    private void OnVelocidadSliderChanged(float value)
    {
        speed = value;
        Debug.Log($"Velocidad actualizada a: {speed}");
    }

    // Método para manejar el cambio de valor del slider de rango
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
        Debug.Log($"Duración actualizada a: {duracion}");
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
            Debug.LogWarning("El TeleportationAnchor 'plataformaTP' no está asignado.");
        }

        if (jugadorRig != null)
        {
            jugadorRig.transform.SetParent(plataforma.transform);
            Debug.Log("Jugador ahora es hijo de la plataforma.");
        }
        else
        {
            Debug.LogWarning("El jugadorRig no está asignado.");
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
            Debug.LogWarning("El TeleportationAnchor 'sueloTP' no está asignado.");
        }

        if (jugadorRig != null)
        {
            jugadorRig.transform.SetParent(null);
            Debug.Log("Jugador liberado de la plataforma.");
        }
        else
        {
            Debug.LogWarning("El jugadorRig no está asignado.");
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

        // Desuscribirse de los eventos de los botones de teletransportación
        if (entradaBtn != null)
        {
            entradaBtn.onClick.RemoveListener(OnEntradaButtonClicked);
        }

        if (salidaBtn != null)
        {
            salidaBtn.onClick.RemoveListener(OnSalidaButtonClicked);
        }
    }
    // Función para abrir (animar la puerta hacia abajo y los escalones hacia arriba)
    public void Abrir()
    {
        if (abierto)
        {
            Debug.Log("La puerta ya está abierta.");
            return;
        }
        StartCoroutine(AnimarPuertaEscalones(1.5f*Vector3.back, Vector3.forward));
        abierto = true;
    }

    // Función para cerrar (animar la puerta hacia arriba y los escalones hacia abajo)
    public void Cerrar()
    {
        if (!abierto)
        {
            Debug.Log("La puerta ya está cerrada.");
            return;
        }
        StartCoroutine(AnimarPuertaEscalones(1.5f * Vector3.forward, Vector3.back));
        abierto = false;
    }

    // Corrutina para animar la puerta y los escalones
    private IEnumerator AnimarPuertaEscalones(Vector3 puertaMovimiento, Vector3 escalonesMovimiento)
    {
        float duracion = 1f; // Duración de la animación en segundos
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

            // Interpolación lineal para mover los objetos
            puertaGO.transform.localPosition = Vector3.Lerp(puertaPosInicial, puertaPosFinal, t);
            escalonGO.transform.localPosition = Vector3.Lerp(escalonPosInicial, escalonPosFinal, t);
            escalonesGO.transform.localPosition = Vector3.Lerp(escalonesPosInicial, escalonesPosFinal, t);

            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        // Asegurarse de que los objetos lleguen exactamente a su posición final
        puertaGO.transform.localPosition = puertaPosFinal;
        escalonGO.transform.localPosition = escalonPosFinal;
        escalonesGO.transform.localPosition = escalonesPosFinal;
    }
    private IEnumerator MoverPuertaHaciaAbajo()
    {
        float duracion = 1f; // Duración de la animación en segundos
        float tiempoTranscurrido = 0f;

        // Posición inicial y final de la puerta
        Vector3 puertaPosInicial = puertaGO.transform.localPosition;
        Vector3 puertaPosFinal = puertaPosInicial + 1.5f * Vector3.back;

        while (tiempoTranscurrido < duracion)
        {
            float t = tiempoTranscurrido / duracion;

            // Interpolación lineal para mover la puerta
            puertaGO.transform.localPosition = Vector3.Lerp(puertaPosInicial, puertaPosFinal, t);

            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        // Asegurarse de que la puerta llegue exactamente a su posición final
        puertaGO.transform.localPosition = puertaPosFinal;
    }
    // Corrutina para regresar al origen en el plano horizontal (manteniendo la altura actual)
    private IEnumerator ReturnToHorizontalOrigin()
    {
        Vector3 startPosition = plataforma.transform.localPosition;
        Vector3 targetPosition = new Vector3(0f, startPosition.y, 0f); // Mantener la altura actual
        float elapsedTime = 0f;

        while (elapsedTime < 1f) // Duración de 1 segundo para regresar al origen horizontal
        {
            plataforma.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, elapsedTime);
            elapsedTime += Time.deltaTime * speed; // Ajustar la velocidad de retorno
            yield return null;
        }

        // Asegurarse de que la posición final sea exactamente el origen horizontal
        plataforma.transform.localPosition = targetPosition;
    }

    // Corrutina para regresar al origen en el plano vertical (manteniendo la posición X y Z actuales)
    private IEnumerator ReturnToVerticalOrigin()
    {
        Vector3 startPosition = plataforma.transform.localPosition;
        Vector3 targetPosition = new Vector3(startPosition.x, 0f, startPosition.z); // Mantener X y Z actuales
        float elapsedTime = 0f;

        while (elapsedTime < 1f) // Duración de 1 segundo para regresar al origen vertical
        {
            plataforma.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, elapsedTime);
            elapsedTime += Time.deltaTime * speed; // Ajustar la velocidad de retorno
            yield return null;
        }

        // Asegurarse de que la posición final sea exactamente el origen vertical
        plataforma.transform.localPosition = targetPosition;
    }
}
