using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Linq;

// Gestor del sistema de Torres de Hanoi con interacción XR
// Controla tres pilas de sockets para recolocar donuts de manera válida
// Valida movimientos, maneja audio de colocación/rechazo, y gestiona popeo de piezas
public class HanoiManager : MonoBehaviour
{
    // Tres pilas de 7 sockets cada una (índice 6 = arriba, índice 0 = abajo)
    [Header("Stacks of 7 sockets (Top = index 6, Bottom = index 0)")]
    public List<XRSocketInteractor> stackA = new List<XRSocketInteractor>();
    public List<XRSocketInteractor> stackB = new List<XRSocketInteractor>();
    public List<XRSocketInteractor> stackC = new List<XRSocketInteractor>();
    
    // Bandera para saltar la validación al mover donuts forzadamente entre sockets
    private bool skipPlacementCheck = false;

    // Configuración de audio para colocación y rechazo de donuts
    [Header("Audio (Hook/Unhook)")]
    
    // Se reproduce cuando un donut es aceptado en la pila (colocación exitosa)
    [Tooltip("Played when a donut is accepted into the stack (hook).")]
    public AudioClip hookClip;
    
    // Se reproduce cuando un donut es rechazado o expulsado de la pila
    [Tooltip("Played when a donut is rejected or popped (unhook).")]
    public AudioClip unhookClip;
    
    // Volumen para el sonido de colocación exitosa
    [Range(0f, 1f)] public float hookVolume = 1f;
    
    // Volumen para el sonido de rechazo/expulsión
    [Range(0f, 1f)] public float unhookVolume = 1f;

    // Fuente de audio; se crea automáticamente si no se asigna
    [Tooltip("If not assigned, an AudioSource will be created on this GameObject.")]
    public AudioSource audioSource;
    
    // Mezcla espacial de audio (1 = totalmente 3D, 0 = mono)
    [Range(0f, 1f)] public float spatialBlend = 1f; // 3D por defecto
    
    // Distancia mínima para atenuación de audio 3D
    public float minDistance = 0.35f;
    
    // Distancia máxima para atenuación de audio 3D
    public float maxDistance = 8f;

    // Asegura que exista una AudioSource válida; la crea si no existe
    private void EnsureAudioSource()
    {
        // Si ya existe, retorna
        if (audioSource != null) return;

        // Intenta obtener una AudioSource existente en el GameObject
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            // Si no existe, la crea dinámicamente
            audioSource = gameObject.AddComponent<AudioSource>();

        // Configura parámetros de la fuente de audio
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = spatialBlend;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
    }

    // Reproduce un clip de audio de una sola vez de forma segura
    private void PlayOneShotSafe(AudioClip clip, float vol)
    {
        // Valida que el clip exista
        if (!clip) return;
        // Asegura que existe una fuente de audio
        EnsureAudioSource();
        // Valida que la fuente fue creada
        if (!audioSource) return;
        // Reproduce el clip a volumen ajustado
        audioSource.PlayOneShot(clip, Mathf.Clamp01(vol));
    }

    // Inicializa el sistema: habilita solo los sockets superiores y configura listeners
    private void Start()
    {
        // Asegura que tengamos una fuente de audio disponible
        EnsureAudioSource();

        // Habilita solo el socket superior de cada pila
        EnableOnlyTopSocket(stackA);
        EnableOnlyTopSocket(stackB);
        EnableOnlyTopSocket(stackC);

        // Configura listeners para cuando se coloca un donut en el socket superior
        stackA[6].selectEntered.AddListener(ctx => OnTopSocketReceived(ctx, stackA));
        stackB[6].selectEntered.AddListener(ctx => OnTopSocketReceived(ctx, stackB));
        stackC[6].selectEntered.AddListener(ctx => OnTopSocketReceived(ctx, stackC));
    }

    // Limpia los listeners de eventos cuando el script se destruye
    private void OnDestroy()
    {
        // Remueve todos los listeners de los sockets superiores
        stackA[6].selectEntered.RemoveAllListeners();
        stackB[6].selectEntered.RemoveAllListeners();
        stackC[6].selectEntered.RemoveAllListeners();
    }

    // Al activar el script, habilita los sockets superiores de cada pila
    private void OnEnable()
    {
        EnableOnlyTopSocket(stackA);
        EnableOnlyTopSocket(stackB);
        EnableOnlyTopSocket(stackC);
    }

    // Al desactivar el script, deshabilita todos los sockets
    private void OnDisable()
    {
        // Desactiva todos los sockets para evitar interacciones
        SetSocketsActive(stackA, false);
        SetSocketsActive(stackB, false);
        SetSocketsActive(stackC, false);
    }

    // Habilita solo el socket superior vacío de la pila, deshabilita el resto
    private void EnableOnlyTopSocket(List<XRSocketInteractor> stack)
    {
        // Desactiva todos los sockets
        foreach (var socket in stack)
        {
            socket.enabled = false;
            // Limpia los listeners previos para evitar acumulación
            socket.selectEntered.RemoveAllListeners();
        }

        // Busca el socket más alto que esté vacío (sin selección)
        for (int i = stack.Count - 1; i >= 0; i--)
        {
            if (!stack[i].hasSelection)
            {
                // Encontró el socket vacío más alto
                XRSocketInteractor newTopSocket = stack[i];
                newTopSocket.enabled = true;

                // Reattacha el listener para detectar cuando se coloque un donut
                newTopSocket.selectEntered.AddListener(ctx => OnTopSocketReceived(ctx, stack));
                break;
            }
        }
    }

    // Activa o desactiva todos los sockets de una pila
    private void SetSocketsActive(List<XRSocketInteractor> stack, bool active)
    {
        foreach (var socket in stack)
            socket.enabled = active;
    }

    // Manejador de evento cuando un donut se coloca en el socket superior
    // Valida el movimiento según las reglas de Torres de Hanoi y mueve a un socket inferior si es válido
    // Si es inválido, rechaza el donut y lo expulsa
    private void OnTopSocketReceived(SelectEnterEventArgs args, List<XRSocketInteractor> stack)
    {
        // Si se debe saltar la validación (movimiento forzado), resetea bandera y retorna
        if (skipPlacementCheck)
        {
            Debug.Log("OnTopSocketReceived skipped due to forced move.");
            skipPlacementCheck = false; // reset flag
            return;
        }

        // Obtiene el donut que se intenta colocar
        IXRSelectInteractable interactable = args.interactableObject;
        GameObject donut = interactable.transform.gameObject;

        // Valida si el movimiento es legal según las reglas de Hanoi
        if (!IsValidMove(donut, stack))
        {
            // El movimiento es inválido: cancela la colocación
            if (args.interactorObject is XRSocketInteractor socket && socket.interactionManager != null)
            {
                // Fuerza la salida del socket
                socket.interactionManager.SelectExit(socket, interactable);
            }

            // Reproduce sonido de rechazo
            PlayOneShotSafe(unhookClip, unhookVolume);

            // Expulsa el donut hacia arriba con movimiento aleatorio
            Rigidbody rb = donut.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddForce(Vector3.up * 3f, ForceMode.Impulse);
            }

            return;
        }

        Debug.Log($"Top socket received {donut.name}, looking for lower slot...");

        XRSocketInteractor targetSlot = GetNextAvailableSlotBelow(stack);
        if (targetSlot != null)
        {
            // Inicia corrutina para mover el donut al socket inferior
            StartCoroutine(SwapDonutToLowerSocket(donut, stack, targetSlot));
        }
        else
        {
            Debug.LogWarning("No available lower slot found.");
        }
    }

    // Encuentra el siguiente socket disponible (vacío) en la pila, desde abajo hacia arriba
    private XRSocketInteractor GetNextAvailableSlotBelow(List<XRSocketInteractor> stack)
    {
        // Busca desde el fondo (0) hasta justo debajo del tope (5)
        for (int i = 0; i < stack.Count - 1; i++)
        {
            if (!stack[i].hasSelection)
                return stack[i];
        }
        return null;
    }

    // Corrutina que mueve un donut desde el socket superior a un socket inferior más bajo
    // Detach, posiciona, reactiva colisiones y anima el movimiento de forma suave
    private IEnumerator SwapDonutToLowerSocket(GameObject donut, List<XRSocketInteractor> stack, XRSocketInteractor targetSocket)
    {
        // Permite que las interacciones actuales se estabilicen en este frame
        yield return null;

        IXRSelectInteractable interactable = donut.GetComponent<IXRSelectInteractable>();
        if (interactable == null)
        {
            Debug.LogWarning("Interactable not found on donut.");
            yield break;
        }

        // 1) Desacopla del socket actual (superior)
        XRSocketInteractor currentTopSocket = stack[stack.Count - 1];
        var manager = currentTopSocket.interactionManager;
        if (manager != null)
        {
            manager.SelectExit(currentTopSocket, interactable);
        }

        // 2) Detiene la física inmediatamente y limpia velocidades
        Rigidbody rb = donut.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 3) Desactiva el colisionador del socket destino temporalmente
        Collider targetCollider = null;
        bool disabledTargetCollider = false;
        if (targetSocket != null)
        {
            targetCollider = targetSocket.GetComponent<Collider>();
            if (targetCollider != null)
            {
                targetCollider.enabled = false;
                disabledTargetCollider = true;
            }
        }

        // 4) Posiciona/rota el donut al transform de attach del socket destino
        Transform attach = targetSocket.attachTransform != null ? targetSocket.attachTransform : targetSocket.transform;
        donut.transform.SetPositionAndRotation(attach.position, attach.rotation);

        // Espera a que la física se actualice antes de hacer select
        yield return new WaitForFixedUpdate();

        // 5) Fuerza el attachment al socket inferior
        if (manager != null)
        {
            manager.SelectEnter(targetSocket, interactable);
        }

        // Reproduce sonido de colocación exitosa cuando se asienta
        PlayOneShotSafe(hookClip, hookVolume);

        // Espera a que el sistema XR finalice el attachment
        yield return null;

        // 6) Reactiva el colisionador del socket destino
        if (disabledTargetCollider && targetCollider != null)
        {
            // Espera un fixed update más para evitar re-colisión inmediata
            yield return new WaitForFixedUpdate();
            targetCollider.enabled = true;
        }

        // 7) Mantiene la física inmóvil mientras está en el socket
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 8) Refresca el estado de la pila e interactividad
        EnableOnlyTopSocket(stack);
        UpdateGrabbableState(stack);

        Debug.Log($"Donut moved to {targetSocket.name}. Stack updated.");
    }

    // Actualiza la capacidad de ser agarrado para cada donut en la pila
    // Solo el donut más superior puede ser agarrado; los demás son inmutables
    private void UpdateGrabbableState(List<XRSocketInteractor> stack)
    {
        bool topmostFound = false;

        // Itera desde arriba (6) hasta abajo (0)
        for (int i = stack.Count - 1; i >= 0; i--)
        {
            XRSocketInteractor socket = stack[i];
            if (socket.hasSelection)
            {
                // Obtiene el donut de este socket
                var interactable = socket.GetOldestInteractableSelected();

                if (interactable is XRGrabInteractable grab)
                {
                    // Si ya encontró el donut superior, desactiva los demás
                    grab.interactionLayers = topmostFound
                        ? InteractionLayerMask.GetMask("") // NO interactable
                        : InteractionLayerMask.GetMask("Default"); // Interactable

                    topmostFound = true;
                    Debug.Log($"{grab.name} -> Layer: {grab.interactionLayers}, Selected: {grab.isSelected}, isKinematic: {grab.GetComponent<Rigidbody>().isKinematic}");
                }
            }
        }
    }

    // Intenta sacar un donut de la pila moviendo todos los de arriba primero y luego expulsándolo
    public void ExitAttempt(GameObject donut)
    {
        Debug.Log($"ExitAttempt: {donut.name} attempting to exit.");

        // Determina en qué pila está el donut y comienza el proceso
        if (stackA.Exists(s => s.hasSelection && s.GetOldestInteractableSelected()?.transform?.gameObject == donut))
        {
            StartCoroutine(MoveDonutToTopAndPop(donut, stackA));
        }
        else if (stackB.Exists(s => s.hasSelection && s.GetOldestInteractableSelected()?.transform?.gameObject == donut))
        {
            StartCoroutine(MoveDonutToTopAndPop(donut, stackB));
        }
        else if (stackC.Exists(s => s.hasSelection && s.GetOldestInteractableSelected()?.transform?.gameObject == donut))
        {
            StartCoroutine(MoveDonutToTopAndPop(donut, stackC));
        }
        else
        {
            Debug.LogWarning($"ExitAttempt: {donut.name} was not found in any stack.");
        }
    }

    // Expulsa el donut más superior de la pila hacia arriba con movimiento aleatorio
    public void PopTopDonut(List<XRSocketInteractor> stack)
    {
        // Busca el donut más superior en la pila
        for (int i = stack.Count - 1; i >= 0; i--)
        {
            var socket = stack[i];
            if (socket.hasSelection)
            {
                // Obtiene el donut de este socket
                IXRSelectInteractable interactable = socket.GetOldestInteractableSelected();
                if (interactable is XRGrabInteractable grab)
                {
                    Rigidbody rb = grab.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        XRSocketInteractor topSocket = stack[stack.Count - 1];

                        // Inicia corrutina de expulsión con delay
                        StartCoroutine(DelayedPop(rb, socket, interactable, topSocket, stack));
                    }
                    break;
                }
            }
        }
    }

    // Corrutina que expulsa un donut con delay y reproduce efectos de audio/física
    private IEnumerator DelayedPop(Rigidbody rb, XRSocketInteractor fromSocket, IXRSelectInteractable interactable, XRSocketInteractor topSocket,
                                   List<XRSocketInteractor> stack)
    {
        // Desactiva temporalmente el socket superior
        topSocket.enabled = false;
        yield return new WaitForSeconds(0.1f);

        // Fuerza la salida del socket actual
        if (fromSocket.interactionManager != null)
        {
            fromSocket.interactionManager.SelectExit(fromSocket, interactable);
        }

        // Reproduce sonido de expulsión
        PlayOneShotSafe(unhookClip, unhookVolume);

        // Activa física para permitir movimiento
        rb.isKinematic = false;

        // Aplica fuerza de expulsión hacia arriba con variación aleatoria
        Vector3 popDirection = Vector3.up * 6f + Random.insideUnitSphere * 1.5f;
        rb.AddForce(popDirection, ForceMode.Impulse);

        yield return new WaitForSeconds(0.5f);

        // Reactiva la capacidad de ser agarrado
        if (interactable is XRGrabInteractable grab)
        {
            grab.interactionLayers = InteractionLayerMask.GetMask("Default");
        }

        // Reactiva el socket superior
        EnableOnlyTopSocket(stack);

        yield return new WaitForSeconds(1.2f);

        // Actualiza el estado de interactividad
        UpdateGrabbableState(stack);
    }

    // Extrae el número del nombre del donut para determinar su tamaño
    // Ej: "Donut3" -> 3
    private int ExtractNumberFromName(string name)
    {
        // Acumula todos los dígitos del nombre
        string numberStr = "";
        foreach (char c in name)
        {
            if (char.IsDigit(c))
                numberStr += c;
        }

        // Intenta convertir a entero
        if (int.TryParse(numberStr, out int value))
        {
            Debug.Log("ExtractNumberFromName: " + name + " -> " + value);
            return value;
        }

        Debug.LogWarning("ExtractNumberFromName: No number found in name: " + name);
        return -1; // No valid number
    }

    // Valida si un movimiento es legal según las reglas de Torres de Hanoi
    // Un donut más grande NO puede colocarse encima de uno más pequeño
    private bool IsValidMove(GameObject incomingDonut, List<XRSocketInteractor> stack)
    {
        // Extrae el tamaño del donut entrante
        int incomingValue = ExtractNumberFromName(incomingDonut.name);
        if (incomingValue == -1)
        {
            Debug.LogWarning("IsValidMove: Could not determine size for " + incomingDonut.name + ", allowing move.");
            return true; // Fail-safe
        }

        Debug.Log("IsValidMove: Checking if " + incomingDonut.name + " (size " + incomingValue + ") can be placed on this stack...");

        // Busca el primer donut ocupado en la pila (comparador)
        for (int i = 0; i < stack.Count; i++)
        {
            if (stack[i].hasSelection)
            {
                GameObject topDonut = stack[i].GetOldestInteractableSelected().transform.gameObject;

                // Ignora el donut mismo para evitar auto-comparación
                if (topDonut == incomingDonut)
                    continue;

                // Obtiene el tamaño del donut superior
                int topValue = ExtractNumberFromName(topDonut.name);
                Debug.Log("IsValidMove: Top donut in stack is " + topDonut.name + " (size " + topValue + ")");

                // Si el donut entrante es más grande, es movimiento inválido
                if (topValue != -1 && incomingValue > topValue)
                {
                    Debug.Log("Invalid move: " + incomingDonut.name + " (size " + incomingValue + ") is larger than " + topDonut.name + " (size " + topValue + ")");
                    return false;
                }
                else
                {
                    Debug.Log("Valid move: " + incomingDonut.name + " can be placed on top of " + topDonut.name);
                }

                break; // Encontró el primer donut real para comparar
            }
        }

        Debug.Log("Valid move: " + incomingDonut.name + " can be placed on an empty stack.");
        return true;
    }

    // Corrutina que mueve un donut a la posición superior de su pila y lo expulsa
    // Se usa cuando el jugador intenta sacar un donut con exit
    private IEnumerator MoveDonutToTopAndPop(GameObject donut, List<XRSocketInteractor> stack)
    {
        // Obtiene el socket superior de la pila
        XRSocketInteractor topSocket = stack[stack.Count - 1];

        IXRSelectInteractable interactable = donut.GetComponent<IXRSelectInteractable>();
        if (interactable == null) yield break;

        // Paso 1: Fuerza la salida del socket actual
        var currentSocket = stack.FirstOrDefault(s => s.hasSelection && s.GetOldestInteractableSelected() == interactable);
        if (currentSocket != null && currentSocket.interactionManager != null)
        {
            currentSocket.interactionManager.SelectExit(currentSocket, interactable);
        }

        // Paso 2: Posiciona en el socket superior sin disparar lógica de placement
        skipPlacementCheck = true;
        Rigidbody rb = donut.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        Transform attach = topSocket.attachTransform != null ? topSocket.attachTransform : topSocket.transform;
        donut.transform.SetPositionAndRotation(attach.position, attach.rotation);

        yield return null;

        // Fuerza el attachment al socket superior
        if (topSocket.interactionManager != null)
        {
            topSocket.interactionManager.SelectEnter(topSocket, interactable);
        }

        yield return null;

        // Reactiva la capacidad de ser agarrado
        if (interactable is XRGrabInteractable grab)
        {
            grab.interactionLayers = InteractionLayerMask.GetMask("Default");
        }

        // Paso 3: Expulsa desde la posición superior
        PopTopDonut(stack);
    }

    // Expulsa secuencialmente todos los donuts de las tres pilas
    public void PopAllPieces()
    {
        Debug.Log("PopAllPieces: Sequentially popping all stacks...");
        StartCoroutine(PopAllPiecesRoutine());
    }

    // Corrutina que vacía todas las pilas expulsando donuts de arriba hacia abajo
    private IEnumerator PopAllPiecesRoutine()
    {
        // Vacía cada pila en orden
        yield return StartCoroutine(PopAllFromStack(stackA));
        yield return StartCoroutine(PopAllFromStack(stackB));
        yield return StartCoroutine(PopAllFromStack(stackC));

        Debug.Log("PopAllPieces: All stacks cleared.");
    }

    // Corrutina que expulsa todos los donuts de una pila individual
    private IEnumerator PopAllFromStack(List<XRSocketInteractor> stack)
    {
        Debug.Log("Popping all donuts from stack (top down, moving each to top before launch)...");

        // Mientras haya donuts en la pila
        while (stack.Any(s => s != null && s.hasSelection))
        {
            // Busca el slot ocupado más alto
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                XRSocketInteractor socket = stack[i];
                if (socket != null && socket.hasSelection)
                {
                    // Obtiene el donut del socket
                    IXRSelectInteractable interactable = socket.GetOldestInteractableSelected();
                    if (interactable != null)
                    {
                        GameObject donut = interactable.transform.gameObject;
                        // Mueve el donut al tope, luego lo expulsa usando lógica existente
                        yield return StartCoroutine(MoveDonutToTopAndPop(donut, stack));

                        yield return new WaitForSeconds(1f);
                    }
                    break; // Rompe el loop interior, recomprueba la pila
                }
            }

            // Pequeño delay entre expulsiones
            yield return new WaitForSeconds(0.05f);
        }
    }

    // Configuración de exhibición: vacía todas las pilas y reorganiza los donuts
    [Header("Exhibition Setup")]
    public Transform piezasParent; // Asignar Piezas en Inspector

    // Inicia el proceso de setup de exhibición
    public void SetupExhibition()
    {
        StartCoroutine(SetupExhibitionRoutine());
    }

    // Corrutina que vacía todas las pilas y reorganiza los donuts en stackA
    // ordenados de mayor a menor tamaño para recrear el estado inicial del juego
    private IEnumerator SetupExhibitionRoutine()
    {
        // Paso 1: Expulsa todo primero
        yield return StartCoroutine(PopAllPiecesRoutine());

        Debug.Log("SetupExhibition: All stacks cleared, preparing to arrange donuts...");

        // Paso 2: Obtiene los donuts del parent de piezas
        List<GameObject> donuts = new List<GameObject>();
        foreach (Transform child in piezasParent)
        {
            // Solo incluye children que tengan XRGrabInteractable
            if (child.GetComponent<XRGrabInteractable>())
            {
                donuts.Add(child.gameObject);
            }
        }

        // Paso 3: Ordena por tamaño (número en nombre)
        donuts.Sort((a, b) => ExtractNumberFromName(b.name).CompareTo(ExtractNumberFromName(a.name)));
        // Ordena de mayor (número más grande) primero para que vaya al fondo
        /*
        // Paso 4: Coloca cada donut en stackA
        foreach (GameObject donut in donuts)
        {
            XRSocketInteractor targetSlot = GetNextAvailableSlotBelow(stackA);
            if (targetSlot != null)
            {
                Debug.Log($"Placing {donut.name} into {targetSlot.name}...");
                yield return StartCoroutine(SwapDonutToLowerSocket(donut, stackA, targetSlot));
                yield return new WaitForSeconds(0.5f); // pequeño delay para estabilizar
            }
            else
            {
                Debug.LogWarning($"No slot available in stackA for {donut.name}");
            }
        }

        Debug.Log("SetupExhibition: Completed stacking in first tower.");*/

        // Paso 4: Coloca cada donut en el socket superior para que el flujo natural ocurra
        foreach (GameObject donut in donuts)
        {
            XRSocketInteractor topSocket = stackA[stackA.Count - 1]; // top is always last
            Debug.Log($"Placing {donut.name} into top socket {topSocket.name}...");

            // Posiciona el donut en el socket
            Rigidbody rb = donut.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Usa el transform de attach si existe, sino el transform del socket
            Transform attach = topSocket.attachTransform != null ? topSocket.attachTransform : topSocket.transform;
            donut.transform.SetPositionAndRotation(attach.position, attach.rotation);

            // Fuerza la selección para disparar OnTopSocketReceived
            if (topSocket.interactionManager != null)
            {
                var interactable = donut.GetComponent<IXRSelectInteractable>();
                topSocket.interactionManager.SelectEnter(topSocket, interactable);
            }

            // Espera un poco para que el flujo natural de drop ocurra
            yield return new WaitForSeconds(0.5f);
        }
    }

}