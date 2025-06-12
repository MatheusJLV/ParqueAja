using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.UI;
using System.Collections;

public class AsientoRotatorio : MonoBehaviour
{
    public GameObject asientoGO; // Referencia al asiento (este objeto)
    public GameObject jugadorRig; // Referencia al jugador (XR Rig)
    public float velocidadMaxima = 100f; // Velocidad m�xima de rotaci�n
    public float aceleracion = 20f; // Aceleraci�n de la rotaci�n
    public float tiempo = 15f; // Duraci�n de la rotaci�n
    public float multiplier = 40f; // multiplicador de la rotaci�n automatizada, es distinta la influencia manual a la automatica

    public TeleportationAnchor asientoTP; // TeleportationAnchor para el asiento
    public TeleportationAnchor sueloTP;   // TeleportationAnchor para el suelo

    // Nuevas variables solicitadas
    public Transform posicionInstancia; // Posici�n donde instanciar la pelota
    public GameObject mecanismo;     // Prefab de la mecanismo
    public GameObject pelotaPrefab;     // Prefab de la pelota
    public GameObject pelotas;          // Contenedor de pelotas
    public GameObject pelotaActual;     // Referencia a la pelota actual
    public bool pelotaNeeded = false;   // Indica si se necesita una pelota
    public bool pelotaWanted = false;    // Indica si se quiere una pelota

    // Sliders y bot�n para control autom�tico
    public Slider velocidadSd;
    public Slider aceleracionSd;
    public Slider tiempoSd;
    public Button iniciarBtn;
    // Botones para ingresar y salir
    public Button ingresarBtn;
    public Button salirBtn;

    private float rotationSpeed = 0f; // Velocidad actual de rotaci�n
    private Coroutine rotacionCoroutine = null;

    private bool canGirar = false;

    void Start()
    {
        if (iniciarBtn != null)
        {
            iniciarBtn.onClick.AddListener(IniciarRotacion);
        }
        if (velocidadSd != null)
        {
            velocidadSd.onValueChanged.AddListener(OnVelocidadSliderChanged);
        }
        if (aceleracionSd != null)
        {
            aceleracionSd.onValueChanged.AddListener(OnAceleracionSliderChanged);
        }
        if (tiempoSd != null)
        {
            tiempoSd.onValueChanged.AddListener(OnTiempoSliderChanged);
        }
        if (ingresarBtn != null)
        {
            ingresarBtn.onClick.AddListener(Ingresar);
        }
        if (salirBtn != null)
        {
            salirBtn.onClick.AddListener(Salir);
        }
    }

    // M�todos para actualizar las variables al cambiar los sliders
    private void OnVelocidadSliderChanged(float value)
    {
        velocidadMaxima = value;
    }

    private void OnAceleracionSliderChanged(float value)
    {
        aceleracion = value;
    }

    private void OnTiempoSliderChanged(float value)
    {
        tiempo = value;
    }

    // M�todo para iniciar la rotaci�n autom�tica
    public void IniciarRotacion()
    {
        if (rotacionCoroutine != null)
        {
            StopCoroutine(rotacionCoroutine);
        }
        rotacionCoroutine = StartCoroutine(RotacionAutomatica());
    }

    private IEnumerator RotacionAutomatica()
    {
        float tiempoTranscurrido = 0f;
        rotationSpeed = 0f;

        // Guardar la rotaci�n inicial (identidad)
        Quaternion rotacionInicial = Quaternion.identity;

        // Fase de aceleraci�n hasta velocidad m�xima
        while (tiempoTranscurrido < tiempo / 4f)
        {
            tiempoTranscurrido += Time.deltaTime;
            rotationSpeed = Mathf.Min(rotationSpeed + multiplier * aceleracion * Time.deltaTime, velocidadMaxima);
            mecanismo.transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime, Space.Self);
            yield return null;
        }

        // Fase de mantener velocidad m�xima
        float tiempoMantener = tiempo / 2f;
        float tiempoMantenerTranscurrido = 0f;
        while (tiempoMantenerTranscurrido < tiempoMantener)
        {
            tiempoMantenerTranscurrido += Time.deltaTime;
            mecanismo.transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime, Space.Self);
            yield return null;
        }

        // Fase de retorno suave a la rotaci�n inicial (identidad)
        float anguloInicio = mecanismo.transform.localEulerAngles.z;
        float anguloFinal = 0f; // identidad
        float sentido = rotationSpeed >= 0 ? 1f : -1f;

        // Calcular la diferencia de �ngulo en el sentido correcto
        float diferencia = Mathf.DeltaAngle(anguloInicio, anguloFinal);
        if (Mathf.Sign(diferencia) != sentido)
        {
            // Si la diferencia no est� en el sentido correcto, ajusta para dar la vuelta completa
            diferencia = -sentido * (360f - Mathf.Abs(diferencia));
        }
        float anguloDestino = anguloInicio + diferencia;

        float duracion = 1.75f; // Duraci�n del retorno, puedes ajustarla

        if (Mathf.Abs(diferencia) > 200f)
        {
            duracion = 2.5f; // Duraci�n del retorno, puedes ajustarla
        }
        else if (Mathf.Abs(diferencia) < 120f)
        {
            duracion = 0.1f; // Duraci�n del retorno, puedes ajustarla
        }


            float elapsedTime = 0f;

        while (elapsedTime < duracion)
        {
            float t = elapsedTime / duracion;
            float anguloActual = Mathf.LerpAngle(anguloInicio, anguloDestino, t);
            Vector3 euler = mecanismo.transform.localEulerAngles;
            mecanismo.transform.localEulerAngles = new Vector3(euler.x, euler.y, anguloActual);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ajuste final exacto
        mecanismo.transform.localRotation = rotacionInicial;
        rotationSpeed = 0f;
    }



    // M�todo para ingresar al asiento y teletransportar
    public void Ingresar()
    {
        if (asientoTP != null)
        {
            asientoTP.RequestTeleport();
        }
        else
        {
            Debug.LogWarning("El TeleportationAnchor 'asientoTP' no est� asignado.");
        }

        if (asientoGO != null && jugadorRig != null)
        {
            jugadorRig.transform.SetParent(asientoGO.transform);
        }
        else
        {
            Debug.LogWarning("asientoGO o jugadorRig no est�n asignados.");
        }
    }

    // M�todo para salir del asiento y teletransportar
    public void Salir()
    {
        if (sueloTP != null)
        {
            sueloTP.RequestTeleport();
        }
        else
        {
            Debug.LogWarning("El TeleportationAnchor 'sueloTP' no est� asignado.");
        }

        if (jugadorRig != null)
        {
            jugadorRig.transform.SetParent(null);
        }
        else
        {
            Debug.LogWarning("jugadorRig no est� asignado.");
        }
    }

    void Update()
    {
        // No se recomienda poner Debug.Log aqu� por rendimiento, pero puedes descomentar si lo necesitas.
        // Debug.Log("M�todo Update() llamado.");
        ControlRotacion();

        // Aplicar la rotaci�n al asiento
        if (rotationSpeed != 0f)
        {
            mecanismo.transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime, Space.Self);
            //InstanciarPelota();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (pelotaActual != null && other.gameObject == pelotaActual)
        {
            pelotaNeeded = true;
            InstanciarPelota();
            //pelotaNeeded = false;
        }
        if (other.CompareTag("Player"))
            canGirar = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Aseg�rate de usar el tag correcto
            canGirar = true;
    }


    private void ControlRotacion()
    {
        if (!canGirar) return;
        //Debug.Log("M�todo ControlRotacion() llamado.");
        // Obtener los dispositivos de la mano derecha
        var rightHandDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);

        bool rightPrimaryPressed = false;
        bool rightSecondaryPressed = false;

        foreach (var device in rightHandDevices)
        {
            device.TryGetFeatureValue(CommonUsages.primaryButton, out rightPrimaryPressed);
            device.TryGetFeatureValue(CommonUsages.secondaryButton, out rightSecondaryPressed);
        }

        if (rightPrimaryPressed)
        {
            // Acelerar en la direcci�n positiva
            rotationSpeed = Mathf.Min(rotationSpeed + aceleracion * Time.deltaTime, velocidadMaxima);
        }
        else if (rightSecondaryPressed)
        {
            // Acelerar en la direcci�n negativa
            rotationSpeed = Mathf.Max(rotationSpeed - aceleracion * Time.deltaTime, -velocidadMaxima);
        }
        else
        {
            // Reducir la velocidad gradualmente hacia 0
            rotationSpeed = Mathf.MoveTowards(rotationSpeed, 0f, aceleracion * Time.deltaTime);
        }
    }

    public void InstanciarPelota()
    {
        if (pelotaNeeded && pelotaWanted && pelotaPrefab != null && posicionInstancia != null && pelotas != null)
        {
            pelotaNeeded = false; // Reiniciar la necesidad de pelota
            GameObject nuevaPelota = Instantiate(
                pelotaPrefab,
                posicionInstancia.position,
                posicionInstancia.rotation,
                posicionInstancia.transform
            );
            pelotaActual = nuevaPelota;
            // Suscribirse al evento select exited del XRGrabInteractable
            XRGrabInteractable grabInteractable = nuevaPelota.GetComponent<XRGrabInteractable>();
            if (grabInteractable != null)
            {
                grabInteractable.selectExited.AddListener(OnPelotaSelectExited);
            }
            else
            {
                Debug.LogWarning("La nueva pelota no tiene un componente XRGrabInteractable.");
            }

        }
    }

    private void OnPelotaSelectExited(SelectExitEventArgs args)
    {
        PelotaLanzada();
    }

    void OnDestroy()
    {
        if (iniciarBtn != null)
        {
            iniciarBtn.onClick.RemoveListener(IniciarRotacion);
        }
        if (velocidadSd != null)
        {
            velocidadSd.onValueChanged.RemoveListener(OnVelocidadSliderChanged);
        }
        if (aceleracionSd != null)
        {
            aceleracionSd.onValueChanged.RemoveListener(OnAceleracionSliderChanged);
        }
        if (tiempoSd != null)
        {
            tiempoSd.onValueChanged.RemoveListener(OnTiempoSliderChanged);
        }
        if (ingresarBtn != null)
        {
            ingresarBtn.onClick.RemoveListener(Ingresar);
        }
        if (salirBtn != null)
        {
            salirBtn.onClick.RemoveListener(Salir);
        }
    }

    public void PelotaLanzada()
    {
        if (pelotaActual != null && pelotas != null)
        {
            // Cambiar el parent de la pelota actual al contenedor de pelotas
            //pelotaActual.transform.SetParent(pelotas.transform);






            // Cambiar las propiedades del Rigidbody
            /*Rigidbody rb = pelotaActual.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = true;
                rb.isKinematic = false;

                rb.mass = 0.2f;
                rb.linearDamping = 0.1f;
                rb.angularDamping = 0.05f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            Debug.Log("Pelota agarrada: parent cambiado y Rigidbody actualizado.");*/
        }
    }
}
