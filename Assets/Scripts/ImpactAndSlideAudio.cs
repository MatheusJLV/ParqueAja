using System.Collections;
using UnityEngine;

// Reproduce sonidos de impacto en colisiones y un bucle corto de deslizamiento mientras hay movimiento tangencial
// Utiliza dos AudioSources distintas: una para impactos, otra para deslizamiento
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class ImpactAndSlideAudio : MonoBehaviour
{
    // Clips de audio
    [Header("Audio Clips")]
    
    // Clip utilizado para impactos (golpes cortos)
    [Tooltip("Clip used for impacts (short hits).")]
    public AudioClip impactClip;
    
    // Clip utilizado para deslizamiento (en bucle)
    [Tooltip("Clip used for sliding (looped).")]
    public AudioClip slideClip;

    // Opción de AudioSource externa para deslizamiento
    [Header("External Audio Source (Optional)")]
    
    // AudioSource externa opcional para usar en deslizamiento en lugar de la local
    [Tooltip("Optional external AudioSource to use for sliding instead of the local one.")]
    public AudioSource externalAudioSource;

    // Configuración de impactos
    [Header("Impact Settings")]
    
    // Fuerza mínima para disparar sonido de impacto
    public float minImpactForce = 0.15f;
    
    // Fuerza máxima para escalar volumen de impacto
    public float maxImpactForce = 3.0f;
    
    // Volumen base para impactos
    [Range(0f, 1f)] public float impactBaseVolume = 1f;
    
    // Tiempo mínimo entre impactos consecutivos
    public float impactCooldown = 0.08f;
    
    // Habilita variación aleatoria de pitch en impactos
    public bool impactRandomizePitch = true;
    
    // Cantidad de variación de pitch
    [Range(0f, 0.35f)] public float impactPitchVariance = 0.12f;
    
    // Escala el volumen según la fuerza del impacto
    public bool impactScaleByForce = true;

    // Configuración de deslizamiento
    [Header("Sliding Settings")]
    
    // Velocidad tangencial mínima para iniciar sonido de deslizamiento
    public float slideThreshold = 0.15f;
    
    // Volumen máximo para deslizamiento
    [Range(0f, 1f)] public float slideMaxVolume = 1f;
    
    // Duración del fade in/out de deslizamiento
    public float slideFadeDuration = 0.25f;
    
    // Tiempo sin movimiento tangencial antes de detener deslizamiento
    public float slideIdleTimeout = 0.5f;
    
    // Ajusta el pitch según la velocidad de deslizamiento
    public bool slidePitchBySpeed = false;
    
    // Pitch mínimo para deslizamiento
    [Range(0.8f, 1.2f)] public float slidePitchMin = 0.95f;
    
    // Pitch máximo para deslizamiento
    [Range(0.8f, 1.2f)] public float slidePitchMax = 1.05f;
    
    // Velocidad de referencia para cálculo de pitch
    public float slidePitchRefSpeed = 0.6f;

    // Controles de características
    [Header("Feature Toggles")]
    
    // Permite reproducir sonidos de impacto
    public bool allowImpactSound = true;
    
    // Permite reproducir sonidos de deslizamiento
    public bool allowSlideSound = true;

    // Estado interno del sistema de audio
    
    // AudioSource para sonidos de impacto
    private AudioSource impactSource;
    
    // AudioSource para sonidos de deslizamiento
    private AudioSource slideSource;
    
    // Tiempo hasta el que se puede disparar el siguiente impacto
    private float nextImpactTime;
    
    // Última vez que se detectó movimiento de deslizamiento
    private float lastSlideTime;
    
    // Indica si el sonido de deslizamiento está activo
    private bool slideActive;
    
    // Referencia a la corrutina de fade actual
    private Coroutine fadeRoutine;

    // Inicializa las fuentes de audio y configuración
    private void Awake()
    {
        // Configuración por defecto (AudioSources locales)
        AudioSource[] sources = GetComponents<AudioSource>();

        // Si no hay suficientes AudioSources, crea las necesarias
        if (sources.Length < 2)
        {
            impactSource = gameObject.AddComponent<AudioSource>();
            slideSource = GetComponent<AudioSource>();
        }
        else
        {
            impactSource = sources[0];
            slideSource = sources[1];
        }

        // Si se proporciona una fuente externa, úsala para el canal de deslizamiento
        if (externalAudioSource != null)
        {
            slideSource = externalAudioSource;
        }

        // Configura la fuente de impacto
        impactSource.playOnAwake = false;
        impactSource.loop = false;
        impactSource.volume = 1f;

        // Configura la fuente de deslizamiento
        slideSource.playOnAwake = false;
        slideSource.loop = true;
        slideSource.volume = 0f;
    }

    // Maneja sonidos de impacto cuando inicia colisión
    private void OnCollisionEnter(Collision c)
    {
        // Verifica si los impactos están habilitados y el clip existe
        if (!allowImpactSound || impactClip == null) return;

        // Obtiene la magnitud de la velocidad relativa del impacto
        float impact = c.relativeVelocity.magnitude;
        
        // Si el impacto es muy débil, no reproduce sonido
        if (impact < minImpactForce) return;

        // Verifica el cooldown para evitar múltiples sonidos muy próximos
        float now = Time.time;
        if (now < nextImpactTime) return;

        // Aplica variación aleatoria de pitch si está habilitada
        if (impactRandomizePitch)
            impactSource.pitch = 1f + Random.Range(-impactPitchVariance, impactPitchVariance);
        else
            impactSource.pitch = 1f;

        // Calcula el volumen del impacto
        float vol = impactBaseVolume;
        // Si está habilitado, escala el volumen según la fuerza del impacto
        if (impactScaleByForce)
        {
            float t = Mathf.InverseLerp(minImpactForce, maxImpactForce, impact);
            vol *= Mathf.Clamp01(t);
        }

        // Reproduce el sonido de impacto
        impactSource.PlayOneShot(impactClip, vol);
        // Establece el próximo tiempo permitido para impacto
        nextImpactTime = now + impactCooldown;
    }

    // Maneja deslizamiento mientras la colisión está activa
    private void OnCollisionStay(Collision c)
    {
        // Verifica si los deslizamientos e impactos están habilitados
        if (!allowSlideSound || !allowImpactSound || slideClip == null) return;

        // Obtiene la velocidad relativa y la normal de contacto
        Vector3 rel = c.relativeVelocity;
        Vector3 n = c.contacts[0].normal;
        
        // Calcula la componente tangencial (perpendicular a la normal de contacto)
        float tangential = (rel - Vector3.Dot(rel, n) * n).magnitude;

        // Si el movimiento tangencial supera el umbral, inicia deslizamiento
        if (tangential > slideThreshold)
        {
            // Registra el último tiempo de deslizamiento detectado
            lastSlideTime = Time.time;

            // Si el deslizamiento no está activo, inicia fade in
            if (!slideActive)
            {
                slideActive = true;
                // Detiene cualquier fade anterior
                if (fadeRoutine != null) StopCoroutine(fadeRoutine);
                // Inicia fade hacia volumen máximo
                fadeRoutine = StartCoroutine(FadeSlide(slideSource.volume, slideMaxVolume, slideFadeDuration, true));
            }

            // Ajusta el pitch según velocidad si está habilitado
            if (slidePitchBySpeed)
            {
                float t = Mathf.Clamp01(tangential / Mathf.Max(0.0001f, slidePitchRefSpeed));
                slideSource.pitch = Mathf.Lerp(slidePitchMin, slidePitchMax, t);
            }
        }
    }

    // Maneja fin de deslizamiento cuando la colisión termina
    private void OnCollisionExit(Collision c)
    {
        // Verifica si los deslizamientos están habilitados
        if (!allowSlideSound) return;

        // Si el deslizamiento estaba activo, inicia fade out
        if (slideActive)
        {
            slideActive = false;
            // Detiene cualquier fade anterior
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            // Inicia fade hacia volumen cero
            fadeRoutine = StartCoroutine(FadeSlide(slideSource.volume, 0f, slideFadeDuration, false));
        }
    }

    // Actualiza el estado cada frame para detectar timeout de deslizamiento
    private void Update()
    {
        // Verifica si los deslizamientos están habilitados
        if (!allowSlideSound) return;
        
        // Si el deslizamiento está activo pero ha pasado el timeout, detiene el sonido
        if (slideActive && Time.time - lastSlideTime > slideIdleTimeout)
        {
            slideActive = false;
            // Detiene cualquier fade anterior
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            // Inicia fade hacia volumen cero
            fadeRoutine = StartCoroutine(FadeSlide(slideSource.volume, 0f, slideFadeDuration, false));
        }
    }

    // Corrutina que anima el fade del volumen de deslizamiento
    private IEnumerator FadeSlide(float from, float to, float duration, bool startIfNeeded)
    {
        // Verifica si los deslizamientos están habilitados
        if (!allowSlideSound) yield break;

        // Si se necesita iniciar, configura el clip y reproduce
        if (startIfNeeded)
        {
            slideSource.clip = slideClip;
            if (!slideSource.isPlaying) slideSource.Play();
        }

        // Registra el tiempo de inicio y asegura duración mínima
        float startTime = Time.realtimeSinceStartup;
        duration = Mathf.Max(0.01f, duration);

        // Anima el volumen durante la duración especificada
        while (true)
        {
            float a = (Time.realtimeSinceStartup - startTime) / duration;
            if (a >= 1f) a = 1f;
            // Interpola linealmente el volumen
            slideSource.volume = Mathf.Lerp(from, to, a);
            if (a >= 1f) break;
            yield return null;
        }

        // Si no se inició el sonido, lo detiene cuando termina el fade
        if (!startIfNeeded && to <= 0.0001f)
        {
            slideSource.volume = 0f;
            slideSource.Stop();
        }
    }

    // Detiene el audio cuando el script se desactiva
    private void OnDisable() => HardStopAudio();
    
    // Detiene el audio cuando el objeto se destruye
    private void OnDestroy() => HardStopAudio();

    // Detiene todos los sonidos de audio inmediatamente
    public void HardStopAudio()
    {
        // Detiene y limpia la corrutina de fade
        if (fadeRoutine != null) { StopCoroutine(fadeRoutine); fadeRoutine = null; }
        
        // Detiene la fuente de impacto
        if (impactSource) impactSource.Stop();
        
        // Detiene la fuente de deslizamiento y resetea volumen
        if (slideSource)
        {
            slideSource.Stop();
            slideSource.volume = 0f;
        }
        
        // Marca deslizamiento como inactivo
        slideActive = false;
    }
}
