using System;
using System.Collections.Generic;
using UnityEngine;

// Sistema de enrutamiento de audio basado en triggers que reproduce AudioSources según los tags de los colliders.
// Mapea eventos de entrada de trigger a fuentes de audio, gestionando dos grupos: "instantáneos" y "continuos".
// Evita reproducciones duplicadas rastreando los colliders que ya han sido detectados.
[DisallowMultipleComponent]
public class TaggedAudioTriggerRouter : MonoBehaviour
{
    // Estructura que asocia un tag con una fuente de audio específica
    [Serializable]
    public struct TagToSource
    {
        public string tag;               // Tag del collider que activará el audio
        public AudioSource source;       // Fuente de audio que se reproducirá
    }

    [Header("Mappings (trigger tag -> audio source)")]
    public TagToSource[] instantaneous;  // Array de mapeos para sonidos instantáneos
    public TagToSource[] continuous;     // Array de mapeos para sonidos continuos

    private Dictionary<string, AudioSource> _instByTag;  // Diccionario de búsqueda rápida para instantáneos
    private Dictionary<string, AudioSource> _contByTag;  // Diccionario de búsqueda rápida para continuos

    private readonly HashSet<Collider> _seen = new HashSet<Collider>();  // Rastrea colliders ya detectados para evitar reproducción repetida

    // Inicializa los diccionarios de mapeo y verifica la presencia de Rigidbody
    private void Awake()
    {
        _instByTag = BuildMap(instantaneous);
        _contByTag = BuildMap(continuous);

        // (Opcional pero recomendado) Asegurarse de que al menos uno del par tenga un Rigidbody
        // La bola ya debería tener uno. Si no, agregarlo aquí o advertir:
        if (GetComponent<Rigidbody>() == null)
            Debug.LogWarning($"{nameof(TaggedAudioTriggerRouter)} on {name} has no Rigidbody; " +
                             "trigger messages require a Rigidbody on one of the colliders.");
    }

    // Construye un diccionario de mapeo rápido tag->AudioSource a partir de un array de TagToSource
    private Dictionary<string, AudioSource> BuildMap(TagToSource[] arr)
    {
        var map = new Dictionary<string, AudioSource>(StringComparer.Ordinal);
        if (arr == null) return map;
        foreach (var e in arr)
        {
            if (!string.IsNullOrWhiteSpace(e.tag) && e.source != null)
                map[e.tag] = e.source; // el último gana si hay duplicados
        }
        return map;
    }

    // Intenta reproducir el audio asociado al tag, buscando en ambos diccionarios (instantáneos y continuos)
    private void TryPlayForTag(string tag)
    {
        if (_instByTag.TryGetValue(tag, out var a) && a != null) a.Play();
        if (_contByTag.TryGetValue(tag, out var b) && b != null) b.Play();
    }

    // Maneja el evento cuando un collider entra en el trigger, reproduce el audio correspondiente al tag del collider
    private void OnTriggerEnter(Collider other)
    {
        // Ruta normal: entrando al límite del volumen del trigger
        _seen.Add(other);
        TryPlayForTag(other.gameObject.tag); // compara el tag del TRIGGER
    }

    // Maneja el caso cuando un objeto aparece dentro del trigger sin pasar por OnTriggerEnter (spawn/teleport)
    private void OnTriggerStay(Collider other)
    {
        // Ruta de entrada tardía: si apareció/teletransportó dentro, OnTriggerEnter nunca se dispara.
        if (_seen.Add(other)) // retorna true si aún no estaba rastreado
        {
            TryPlayForTag(other.gameObject.tag);
        }
        // de lo contrario no hacer nada; ya fue manejado
    }

    // Maneja el evento cuando un collider sale del trigger, permite que se vuelva a activar en futuras entradas
    private void OnTriggerExit(Collider other)
    {
        // Permite futuras reactivaciones cuando volvamos a entrar
        _seen.Remove(other);
    }
}
