using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// este script ya no se va a usar, queda ahi para borrar luego si por si las dudas

public class SubeYBajaScript : MonoBehaviour
{
    public GameObject baseGO;
    public GameObject base2GO;
    public GameObject puntaGO;

    private Coroutine moverCoroutine;
    public void MoverDesdeBase()
    {
        if (moverCoroutine != null) StopCoroutine(moverCoroutine);
        moverCoroutine = StartCoroutine(TranslacionBase2());
    }

    public void MoverDesdePunta()
    {
        if (moverCoroutine != null) StopCoroutine(moverCoroutine);
        moverCoroutine = StartCoroutine(TranslacionPunta2());
    }
    public IEnumerator TranslacionPunta2()
    {
        if (puntaGO == null)
        {
            Debug.LogWarning("[SubeYBajaScript][TranslacionPunta2] puntaGO es null");
            yield break;
        }

        XRSocketInteractor socket = puntaGO.GetComponent<XRSocketInteractor>();
        if (socket == null)
        {
            Debug.LogWarning("[SubeYBajaScript][TranslacionPunta2] XRSocketInteractor no encontrado en puntaGO");
            yield break;
        }

        if (socket.interactablesSelected.Count > 0)
        {
            var interactable = socket.interactablesSelected[0];
            if (interactable != null)
            {
                GameObject seleccionado = interactable.transform.gameObject;
                Debug.Log("[SubeYBajaScript][TranslacionPunta2] Objeto seleccionado en puntaGO: " + seleccionado.name);

                if (seleccionado.name == "Cono")
                {
                    yield return new WaitForSeconds(0.4f); // Pausa antes de habilitar RollTo

                    RollTo rollTo = seleccionado.GetComponent<RollTo>();
                    if (rollTo != null)
                    {
                        rollTo.enabled = true;
                        Debug.Log("[SubeYBajaScript][TranslacionPunta2] RollTo habilitado en Cono");
                        // Desactivar los sockets de baseGO, base2GO y puntaGO
                        if (baseGO != null)
                        {
                            var socketBase = baseGO.GetComponent<XRSocketInteractor>();
                            if (socketBase != null) socketBase.enabled = false;
                        }
                        if (base2GO != null)
                        {
                            var socketBase2 = base2GO.GetComponent<XRSocketInteractor>();
                            if (socketBase2 != null) socketBase2.enabled = false;
                        }
                        if (puntaGO != null)
                        {
                            var socketPunta = puntaGO.GetComponent<XRSocketInteractor>();
                            if (socketPunta != null) socketPunta.enabled = false;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[SubeYBajaScript][TranslacionPunta2] RollTo no encontrado en Cono");
                    }
                }
            }
        }
        else
        {
            Debug.Log("[SubeYBajaScript][TranslacionPunta2] No hay objetos seleccionados en puntaGO");
        }
        yield break;
    }

    public IEnumerator TranslacionBase2()
    {
        if (baseGO == null)
        {
            Debug.LogWarning("[SubeYBajaScript][TranslacionBase2] baseGO es null");
            yield break;
        }

        XRSocketInteractor socket = baseGO.GetComponent<XRSocketInteractor>();
        if (socket == null)
        {
            Debug.LogWarning("[SubeYBajaScript][TranslacionBase2] XRSocketInteractor no encontrado en baseGO");
            yield break;
        }

        if (socket.interactablesSelected.Count > 0)
        {
            var interactable = socket.interactablesSelected[0];
            if (interactable != null)
            {
                GameObject seleccionado = interactable.transform.gameObject;
                Debug.Log("[SubeYBajaScript][TranslacionBase2] Objeto seleccionado en baseGO: " + seleccionado.name);

                if (seleccionado.name == "Cilindro")
                {
                    yield return new WaitForSeconds(0.4f); // Pausa antes de habilitar RollTo

                    RollTo rollTo = seleccionado.GetComponent<RollTo>();
                    if (rollTo != null)
                    {
                        rollTo.enabled = true;
                        Debug.Log("[SubeYBajaScript][TranslacionBase2] RollTo habilitado en Cilindro");
                        // Desactivar los sockets de baseGO, base2GO y puntaGO
                        if (baseGO != null)
                        {
                            var socketBase = baseGO.GetComponent<XRSocketInteractor>();
                            if (socketBase != null) socketBase.enabled = false;
                        }
                        if (base2GO != null)
                        {
                            var socketBase2 = base2GO.GetComponent<XRSocketInteractor>();
                            if (socketBase2 != null) socketBase2.enabled = false;
                        }
                        if (puntaGO != null)
                        {
                            var socketPunta = puntaGO.GetComponent<XRSocketInteractor>();
                            if (socketPunta != null) socketPunta.enabled = false;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[SubeYBajaScript][TranslacionBase2] RollTo no encontrado en Cilindro");
                    }
                }
            }
        }
        else
        {
            Debug.Log("[SubeYBajaScript][TranslacionBase2] No hay objetos seleccionados en baseGO");
        }
        yield break;
    }


    
    
    
    
    
    
    /*public GameObject baseGO;
    public GameObject base2GO;
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
                // Desactivar gravedad, poner kinematic y desactivar el CapsuleCollider
                Rigidbody rb = seleccionado.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.useGravity = false;
                    Debug.Log("ATENCION [SubeYBajaScript][TranslacionBase] useGravity = false en " + seleccionado.name);

                    rb.isKinematic = true;
                    Debug.Log("ATENCION [SubeYBajaScript][TranslacionBase] isKinematic = true en " + seleccionado.name);

                    rb.angularDamping = 0f;
                }

                // Espera breve para asegurar alineación del socket (ajusta si es necesario)
                //yield return new WaitForSeconds(0.2f);

                //rollTo.meta = puntaGO;


                //rollTo.speed = speed;
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

                // Desactivar gravedad, poner kinematic y desactivar el CapsuleCollider
                Rigidbody rb = seleccionado.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.useGravity = false;
                    Debug.Log("ATENCION [SubeYBajaScript][TranslacionPunta] useGravity = false en " + seleccionado.name);

                    rb.isKinematic = true;
                    Debug.Log("ATENCION [SubeYBajaScript][TranslacionPunta] isKinematic = true en " + seleccionado.name);

                    rb.angularDamping = 0f;
                }


                // Espera breve para asegurar alineación del socket (ajusta si es necesario)
                //yield return new WaitForSeconds(0.2f);

                //rollTo.meta = baseGO; // El destino es la base


                //rollTo.speed = speed;
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
    }*/
}
