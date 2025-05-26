using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SubeYBajaScript : MonoBehaviour
{
    public GameObject baseGO;
    public GameObject puntaGO;
    public GameObject cilindro;
    public GameObject cono;
    public bool baseBool = false;
    public bool puntaBool = false;
    public GameObject objeto;

    public float speed = 2f;

    private Coroutine moverCoroutine;

    public void MoverDesdeBase()
    {
        if (moverCoroutine != null) StopCoroutine(moverCoroutine);
        moverCoroutine = StartCoroutine(TranslacionBase());
    }

    public void MoverDesdePunta()
    {
        if (moverCoroutine != null) StopCoroutine(moverCoroutine);
        moverCoroutine = StartCoroutine(TranslacionPunta());
    }

    private IEnumerator TranslacionBase()
    {

        yield return new WaitForSeconds(0.5f);


        XRSocketInteractor socketPunta = puntaGO.GetComponent<XRSocketInteractor>();
        Debug.Log("[SubeYBajaScript] TranslacionBase llamado");

        if (baseGO == null || puntaGO == null)
        {
            Debug.LogWarning("[SubeYBajaScript] baseGO o puntaGO es null");
            yield break;
        }

        XRSocketInteractor socket = baseGO.GetComponent<XRSocketInteractor>();
        if (socket == null)
        {
            Debug.LogWarning("[SubeYBajaScript] XRSocketInteractor no encontrado en baseGO");
            yield break;
        }

        GameObject seleccionado = null;
        if (socket.interactablesSelected.Count > 0)
        {
            var interactable = socket.interactablesSelected[0];
            if (interactable != null)
            {
                seleccionado = interactable.transform.gameObject;
                Debug.Log("[SubeYBajaScript] Objeto seleccionado en baseGO: " + seleccionado.name);
            }
        }
        else
        {
            Debug.Log("[SubeYBajaScript] No hay objetos seleccionados en baseGO");
        }

        if (seleccionado != null && seleccionado.name == "Cilindro" && !puntaBool)
        {
            Debug.Log("[SubeYBajaScript] Condiciones cumplidas para mover desde base a punta");

            RollTo rollTo = seleccionado.GetComponent<RollTo>();
            XRSocketInteractor socketBase = baseGO.GetComponent<XRSocketInteractor>();
            var interactable = seleccionado.GetComponent<XRGrabInteractable>();

            if (rollTo != null)
            {
                // Forzar unsocket y desactivar el socket
                if (socketBase != null && interactable != null && socketBase.interactablesSelected.Contains(interactable))
                {
                    socketBase.interactionManager.SelectExit((IXRSelectInteractor)socketBase, (IXRSelectInteractable)interactable);
                    socketBase.enabled = false;
                    Debug.Log("[SubeYBajaScript] Unsocket y socket desactivado en baseGO");
                }

                // Espera breve para asegurar alineación del socket (ajusta si es necesario)
                yield return new WaitForSeconds(0.2f);

                //rollTo.meta = puntaGO;
                rollTo.speed = speed;
                rollTo.enabled = true;
            }
            else
            {
                Debug.LogWarning("[SubeYBajaScript] RollTo no encontrado en el objeto seleccionado");
            }
        }
        else
        {
            Debug.Log("[SubeYBajaScript] Condiciones NO cumplidas para mover desde base a punta");
        }
    }

    private IEnumerator TranslacionPunta()
    {

        yield return new WaitForSeconds(0.5f);


        Debug.Log("[SubeYBajaScript] TranslacionPunta llamado");

        if (puntaGO == null || baseGO == null)
        {
            Debug.LogWarning("[SubeYBajaScript] puntaGO o baseGO es null");
            yield break;
        }

        XRSocketInteractor socket = puntaGO.GetComponent<XRSocketInteractor>();
        if (socket == null)
        {
            Debug.LogWarning("[SubeYBajaScript] XRSocketInteractor no encontrado en puntaGO");
            yield break;
        }

        GameObject seleccionado = null;
        if (socket.interactablesSelected.Count > 0)
        {
            var interactable = socket.interactablesSelected[0];
            if (interactable != null)
            {
                seleccionado = interactable.transform.gameObject;
                Debug.Log("[SubeYBajaScript] Objeto seleccionado en puntaGO: " + seleccionado.name);
            }
        }
        else
        {
            Debug.Log("[SubeYBajaScript] No hay objetos seleccionados en puntaGO");
        }

        if (seleccionado != null && seleccionado.name == "Cono" && !baseBool)
        {
            Debug.Log("[SubeYBajaScript] Condiciones cumplidas para mover desde punta a base");

            RollTo rollTo = seleccionado.GetComponent<RollTo>();
            XRSocketInteractor socketPunta = puntaGO.GetComponent<XRSocketInteractor>();
            var interactable = seleccionado.GetComponent<XRGrabInteractable>();

            if (rollTo != null)
            {
                // Forzar unsocket y desactivar el socket de puntaGO
                if (socketPunta != null && interactable != null && socketPunta.interactablesSelected.Contains(interactable))
                {
                    socketPunta.interactionManager.SelectExit((IXRSelectInteractor)socketPunta, (IXRSelectInteractable)interactable);
                    socketPunta.enabled = false;
                    Debug.Log("[SubeYBajaScript] Unsocket y socket desactivado en puntaGO");
                }

                // Espera breve para asegurar alineación del socket (ajusta si es necesario)
                yield return new WaitForSeconds(0.2f);

                //rollTo.meta = baseGO; // El destino es la base
                rollTo.speed = speed;
                rollTo.enabled = true;
            }
            else
            {
                Debug.LogWarning("[SubeYBajaScript] RollTo no encontrado en el objeto seleccionado");
            }
        }
        else
        {
            Debug.Log("[SubeYBajaScript] Condiciones NO cumplidas para mover desde punta a base");
        }
    }
}
