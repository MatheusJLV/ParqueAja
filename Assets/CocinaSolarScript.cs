using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.UI;
using System.Collections;

public class CocinaSolarScript : MonoBehaviour
{
    public GameObject asientoGO; // Referencia al asiento (la pelota que aloja al jugador)
    public GameObject jugadorRig; // XR Rig del jugador
    public TeleportationAnchor asientoTP; // TeleportationAnchor para el asiento
    public TeleportationAnchor sueloTP;   // TeleportationAnchor para el suelo
    public GameObject pelotaPlayerPrefab; // Prefab de la pelota que aloja al jugador
    public GameObject puntoInstanciaPelota; // Referencia vacía para la posición de la pelota

    public Button ingresarBtn;
    public Button salirBtn;

    public Slider duracionSD;
    public int duracion = 5;

    private Vector3 jugadorRigOriginalWorldScale;
    private bool playerDentro = false;

    private Coroutine temporizadorCoroutine;

    public float range = 0.8f;

    public GameObject foco; // Asigna el foco desde el editor

    void Start()
    {
        if (ingresarBtn != null)
            ingresarBtn.onClick.AddListener(Ingresar);

        if (salirBtn != null)
            salirBtn.onClick.AddListener(Salir);

        if (duracionSD != null)
            duracionSD.onValueChanged.AddListener(ChangeDuracion);

        // Inicializa el valor del slider si es necesario
        if (duracionSD != null)
            duracionSD.value = duracion;
    }

    public void ChangeDuracion(float value)
    {
        duracion = Mathf.RoundToInt(value);
    }

    public void Ingresar()
    {
        if (playerDentro)
            return;

        // Generar un offset aleatorio en cada eje dentro del rango especificado
        Vector3 randomOffset = new Vector3(
            Random.Range(-range, range),
            Random.Range(-range, range),
            Random.Range(-range, range)
        );

        // Instanciar la pelota si no existe
        if (asientoGO == null && pelotaPlayerPrefab != null && puntoInstanciaPelota != null)
        {
            asientoGO = Instantiate(
                pelotaPlayerPrefab,
                puntoInstanciaPelota.transform.position + randomOffset,
                puntoInstanciaPelota.transform.rotation,
                transform
            );

            // Asignar el foco si el script está presente
            CanicaSolarScript canica = asientoGO.GetComponent<CanicaSolarScript>();
            if (canica != null)
                canica.foco = foco;

            // Buscar el TeleportationAnchor en la pelota
            TeleportationAnchor anchor = asientoGO.GetComponentInChildren<TeleportationAnchor>();
            if (anchor != null)
                asientoTP = anchor;
        }

        // Teletransportar al asiento
        if (asientoTP != null)
            asientoTP.RequestTeleport();

        // Reparentar y escalar el XR Rig
        if (asientoGO != null && jugadorRig != null)
        {
            jugadorRigOriginalWorldScale = jugadorRig.transform.lossyScale;
            jugadorRig.transform.SetParent(asientoGO.transform);
            jugadorRig.transform.localPosition = Vector3.zero;
            jugadorRig.transform.localRotation = Quaternion.identity;
            SetWorldScale(jugadorRig.transform, jugadorRigOriginalWorldScale / 100f);
        }

        playerDentro = true;
        if (ingresarBtn != null)
            ingresarBtn.interactable = false;

        // Iniciar temporizador
        if (temporizadorCoroutine != null)
            StopCoroutine(temporizadorCoroutine);
        temporizadorCoroutine = StartCoroutine(Temporizador());
    }

    private IEnumerator Temporizador()
    {
        yield return new WaitForSeconds(duracion);
        Salir();
    }

    public void Salir()
    {
        // Teletransportar al suelo
        if (sueloTP != null)
            sueloTP.RequestTeleport();

        // Restaurar el XR Rig
        if (jugadorRig != null)
        {
            jugadorRig.transform.SetParent(null);
            SetWorldScale(jugadorRig.transform, jugadorRigOriginalWorldScale);
        }

        playerDentro = false;
        if (ingresarBtn != null)
            ingresarBtn.interactable = true;

        // Detener temporizador si está corriendo
        if (temporizadorCoroutine != null)
        {
            StopCoroutine(temporizadorCoroutine);
            temporizadorCoroutine = null;
        }

        // Destruir asientoGO al final
        if (asientoGO != null)
        {
            Destroy(asientoGO);
            asientoGO = null;
        }
    }

    // Establecer la escala local para una escala global deseada
    void SetWorldScale(Transform t, Vector3 worldScale)
    {
        if (t.parent)
        {
            Vector3 parentScale = t.parent.lossyScale;
            t.localScale = new Vector3(
                worldScale.x / parentScale.x,
                worldScale.y / parentScale.y,
                worldScale.z / parentScale.z
            );
        }
        else
        {
            t.localScale = worldScale;
        }
    }
}

