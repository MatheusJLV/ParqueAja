using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class CocinaSolarScript : MonoBehaviour
{
    public GameObject asientoGO;
    public GameObject jugadorRig;
    public TeleportationAnchor asientoTP;
    public TeleportationAnchor sueloTP;
    public GameObject pelotaPlayerPrefab;
    public GameObject puntoInstanciaPelota;
    public Button ingresarBtn;
    public Button salirBtn;
    public Slider duracionSD;
    public int duracion = 5;
    public float range = 0.8f;
    public GameObject foco;

    private Vector3 jugadorRigOriginalWorldScale;
    private bool playerDentro = false;
    private Coroutine temporizadorCoroutine;

    void Start()
    {
        if (ingresarBtn != null) ingresarBtn.onClick.AddListener(() => StartCoroutine(Ingresar()));
        if (salirBtn != null) salirBtn.onClick.AddListener(Salir);
        if (duracionSD != null)
        {
            duracionSD.onValueChanged.AddListener(ChangeDuracion);
            duracionSD.value = duracion;
        }
    }

    public void ChangeDuracion(float value)
    {
        duracion = Mathf.RoundToInt(value);
    }

    private IEnumerator Ingresar()
    {
        if (playerDentro) yield break;

        // Generate random spawn offset
        Vector3 randomOffset = new Vector3(
            Random.Range(-range, range),
            Random.Range(-range, range),
            Random.Range(-range, range)
        );

        Rigidbody rb = null;
        Collider col = null;

        // Instantiate the ball
        if (asientoGO == null && pelotaPlayerPrefab != null && puntoInstanciaPelota != null)
        {
            asientoGO = Instantiate(
                pelotaPlayerPrefab,
                puntoInstanciaPelota.transform.position + randomOffset,
                puntoInstanciaPelota.transform.rotation,
                transform
            );

            // Assign spotlight to internal script
            CanicaSolarScript canica = asientoGO.GetComponent<CanicaSolarScript>();
            if (canica != null)
                canica.foco = foco;

            TeleportationAnchor anchor = asientoGO.GetComponentInChildren<TeleportationAnchor>();
            if (anchor != null)
                asientoTP = anchor;

            // Temporarily disable physics
            rb = asientoGO.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            col = asientoGO.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        // Wait a moment before teleport
        yield return new WaitForSeconds(0.1f);
        yield return new WaitForEndOfFrame();

        // Teleport the player to the ball
        if (asientoTP != null) asientoTP.RequestTeleport();

        // Wait for teleport to apply
        yield return new WaitForSeconds(0.1f);
        yield return new WaitForEndOfFrame();

        // Reparent and scale XR Rig
        if (asientoGO != null && jugadorRig != null)
        {
            jugadorRigOriginalWorldScale = jugadorRig.transform.lossyScale;
            jugadorRig.transform.SetParent(asientoGO.transform);
            jugadorRig.transform.localPosition = Vector3.zero;
            jugadorRig.transform.localRotation = Quaternion.identity;
            SetWorldScale(jugadorRig.transform, jugadorRigOriginalWorldScale / 100f);

            // Disable CharacterController
            var cc = jugadorRig.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
        }

        // Wait before re-enabling physics
        yield return new WaitForSeconds(0.3f);

        if (rb != null) rb.isKinematic = false;
        if (col != null) col.enabled = true;

        playerDentro = true;
        if (ingresarBtn != null) ingresarBtn.interactable = false;

        // Start timer
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
        // Teleport to floor
        if (sueloTP != null)
            sueloTP.RequestTeleport();

        // Restore XR Rig
        if (jugadorRig != null)
        {
            jugadorRig.transform.SetParent(null);
            SetWorldScale(jugadorRig.transform, jugadorRigOriginalWorldScale);

            // Re-enable CharacterController
            var cc = jugadorRig.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;
        }

        playerDentro = false;
        if (ingresarBtn != null) ingresarBtn.interactable = true;

        if (temporizadorCoroutine != null)
        {
            StopCoroutine(temporizadorCoroutine);
            temporizadorCoroutine = null;
        }

        // Destroy the ball
        if (asientoGO != null)
        {
            Destroy(asientoGO);
            asientoGO = null;
        }
    }

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
