using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.VFX;

/*
 Controla la interacción del jugador con una cocina solar.
 Permite ingresar a una “canica/asiento”, temporizar la experiencia
 y restaurar al jugador a su estado original.
*/

public class CocinaSolarScript : MonoBehaviour
{
     // Objeto que actúa como asiento o canica
    public GameObject asientoGO;
    // XR Rig del jugador
    public GameObject jugadorRig;
    // Puntos de teletransportación
    public TeleportationAnchor asientoTP;
    public TeleportationAnchor sueloTP;
    // Prefab de la canica y punto de instanciación
    public GameObject pelotaPlayerPrefab;
    public GameObject puntoInstanciaPelota;
    // Controles de interfaz
    public Button ingresarBtn;
    //public Button salirBtn;
    public Slider duracionSD;
    // Parámetros de tiempo y dispersión
    public int duracion = 5;
    public float range = 0.8f;
    // Foco de iluminación asociado a la canica
    public GameObject foco;
    // Escala original del XR Rig
    private Vector3 jugadorRigOriginalWorldScale;
    // Estado del jugador
    private bool playerDentro = false;
    // Control del temporizador
    private Coroutine temporizadorCoroutine;

    // NUEVO: botón para toggle de partículas
    public Button iniciarAnimacionBtn;

    // NUEVO: referencia al componente Visual Effect (arrástralo desde la escena)
    public VisualEffect vfx;

    // NUEVO: estado local (no dependemos de aliveParticleCount porque es async)
    private bool vfxEncendido = false;


    void Start()
    {
        // Ensure VFX starts OFF
        if (vfx != null)
        {
            vfx.Stop();
            // Optional: hard reset so no residual particles remain
            // vfx.Reinit();
        }
        vfxEncendido = false;

        // Asigna acciones a los botones
        if (ingresarBtn != null) ingresarBtn.onClick.AddListener(() => StartCoroutine(Ingresar()));
        //if (salirBtn != null) salirBtn.onClick.AddListener(Salir);

        if (iniciarAnimacionBtn != null)
            iniciarAnimacionBtn.onClick.AddListener(StartAnimation);


        // Configura el slider de duración
        /*if (duracionSD != null)
        {
            duracionSD.onValueChanged.AddListener(ChangeDuracion);
            duracionSD.value = duracion;
        }*/
    }

    public void StartAnimation()
    {
        if (vfx == null)
        {
            Debug.LogWarning("[CocinaSolarScript] Falta asignar 'vfx' (VisualEffect) en el inspector.");
            return;
        }

        if (!vfxEncendido)
        {
            vfx.Play();          // inicia (OnPlay por defecto)
            vfxEncendido = true;
        }
        else
        {
            vfx.Stop();          // detiene spawns (equivale a enviar "OnStop")
            vfxEncendido = false;

            // Opcional: si quieres cortar de inmediato y limpiar, descomenta:
            // vfx.Reinit();     // reinicia tiempo y re-envía el evento inicial cuando corresponda
        }
    }

    // Actualiza la duración desde el slider
    /*public void ChangeDuracion(float value)
    {
        duracion = Mathf.RoundToInt(value);
    }*/

    // Proceso de ingreso del jugador a la canica
    private IEnumerator Ingresar()
    {
        if (playerDentro) yield break;

        // Genera un desplazamiento aleatorio de aparición
        Vector3 randomOffset = new Vector3(
            Random.Range(-range, range),
            Random.Range(-range, range),
            Random.Range(-range, range)
        );

        Rigidbody rb = null;
        Collider col = null;

        // Instancia la canica si no existe
        if (asientoGO == null && pelotaPlayerPrefab != null && puntoInstanciaPelota != null)
        {
            asientoGO = Instantiate(
                pelotaPlayerPrefab,
                puntoInstanciaPelota.transform.position + randomOffset,
                puntoInstanciaPelota.transform.rotation,
                transform
            );

            // Asigna el foco al script interno
            CanicaSolarScript canica = asientoGO.GetComponent<CanicaSolarScript>();
            if (canica != null)
                canica.foco = foco;

            // Obtiene el punto de teletransporte interno
            TeleportationAnchor anchor = asientoGO.GetComponentInChildren<TeleportationAnchor>();
            if (anchor != null)
                asientoTP = anchor;

            // Desactiva físicas temporalmente
            rb = asientoGO.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            col = asientoGO.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        // Espera breve antes de teletransportar
        yield return new WaitForSeconds(0.1f);
        yield return new WaitForEndOfFrame();

        // Teletransporta al jugador al asiento
        if (asientoTP != null) asientoTP.RequestTeleport();

        // Wait for teleport to apply
        yield return new WaitForSeconds(0.1f);
        yield return new WaitForEndOfFrame();

        // Reparenta y ajusta el XR Rig
        if (asientoGO != null && jugadorRig != null)
        {
            jugadorRigOriginalWorldScale = jugadorRig.transform.lossyScale;
            jugadorRig.transform.SetParent(asientoGO.transform);
            jugadorRig.transform.localPosition = Vector3.zero;
            jugadorRig.transform.localRotation = Quaternion.identity;
            SetWorldScale(jugadorRig.transform, jugadorRigOriginalWorldScale / 100f);

            // Desactiva el CharacterController
            var cc = jugadorRig.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
        }

        // Reactiva físicas
        yield return new WaitForSeconds(0.3f);

        if (rb != null) rb.isKinematic = false;
        if (col != null) col.enabled = true;

        playerDentro = true;
        if (ingresarBtn != null) ingresarBtn.interactable = false;

        // Inicia temporizador
        if (temporizadorCoroutine != null)
            StopCoroutine(temporizadorCoroutine);
        temporizadorCoroutine = StartCoroutine(Temporizador());
    }

    // Temporizador de permanencia
    private IEnumerator Temporizador()
    {
        yield return new WaitForSeconds(duracion);
        Salir();
    }

    // Proceso de salida del jugador
    public void Salir()
    {
        // Teletransporta al suelo
        if (sueloTP != null)
            sueloTP.RequestTeleport();

        // Restaura el XR Rig
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

         // Detiene temporizador
        if (temporizadorCoroutine != null)
        {
            StopCoroutine(temporizadorCoroutine);
            temporizadorCoroutine = null;
        }

        // Destruye la canica
        if (asientoGO != null)
        {
            Destroy(asientoGO);
            asientoGO = null;
        }
    }

    // Ajusta la escala mundial respetando la jerarquía
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
