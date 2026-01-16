using UnityEngine;
using UnityEngine.VFX;

// Sistema automático de parada de efectos visuales que detiene un VisualEffect después de un tiempo especificado.
// Soporta tiempo escalado y no escalado, reinicio automático al activarse, y limpieza inmediata de partículas.
[DisallowMultipleComponent]
public class VFXAutoStop : MonoBehaviour
{
    [Header("Timing")]
    [Min(0f)] public float seconds = 8f;   // Duración en segundos que el VFX debe estar activo
    public bool useUnscaledTime = true;    // Ignorar Time.timeScale (recomendado para efectos independientes)

    [Header("Behavior")]
    public bool playOnEnable = true;       // Reproducir automáticamente cuando este componente se habilita
    public bool clearOnStop = false;       // Reinicializar para limpiar partículas instantáneamente al detener
    public bool searchChildrenIfMissing = false; // Buscar un VFX en los hijos si no se encuentra en este objeto

    VisualEffect vfx;                      // Referencia al componente VisualEffect                      // Referencia al componente VisualEffect

    // Obtiene la referencia al componente VisualEffect, buscando en los hijos si es necesario
    void Awake()
    {
        // Obtener el VFX en este objeto; opcionalmente buscar en los hijos
        vfx = GetComponent<VisualEffect>();
        if (!vfx && searchChildrenIfMissing)
            vfx = GetComponentInChildren<VisualEffect>(true);
    }

    // Maneja la activación del componente, reproduciendo el VFX e iniciando el temporizador de parada
    void OnEnable()
    {
        if (!vfx) { vfx = GetComponent<VisualEffect>(); if (!vfx) return; }

        if (playOnEnable)
        {
            // (Opcional) asegurar un inicio limpio
            // vfx.Reinit();   // descomenta si quieres un reinicio completo cada vez
            vfx.Play();
        }

        StartTimer();
    }

    // Cancela la parada programada y detiene las corrutinas al deshabilitar el componente
    void OnDisable()
    {
        CancelInvoke(nameof(DoStop));
        StopAllCoroutines();
    }

    // Inicia o reinicia el temporizador de cuenta regresiva para detener el VFX (puede llamarse manualmente)
    public void StartTimer()
    {
        CancelInvoke(nameof(DoStop));
        if (useUnscaledTime) Invoke(nameof(DoStop), seconds);
        else StartCoroutine(StopAfterScaled(seconds));
    }

    // Corrutina que espera un tiempo escalado antes de detener el VFX
    System.Collections.IEnumerator StopAfterScaled(float s)
    {
        yield return new WaitForSeconds(s);  // escalado por timeScale
        DoStop();
    }

    // Detiene el VFX deteniendo todos los spawners; las partículas existentes se terminan por su tiempo de vida
    public void DoStop()
    {
        if (!vfx) return;
        vfx.Stop();            // detiene todos los spawners; las partículas existentes terminan por su duración
        if (clearOnStop) vfx.Reinit();  // limpieza inmediata (opcional)
    }
}

