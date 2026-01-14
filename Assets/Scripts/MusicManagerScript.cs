using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Gestor de música: reproduce clips de audio de diferentes categorías con controles de reproducción y volumen.
public class MusicManagerScript : MonoBehaviour
{
    [SerializeField]
    private List<AudioClip> musicClips; // Lista de música actual en reproducción

    [SerializeField]
    private List<AudioClip> Accion; // Música de categoría Acción

    [SerializeField]
    private List<AudioClip> Favoritas; // Música de categoría Favoritas

    [SerializeField]
    private List<AudioClip> Relax; // Música de categoría Relax

    [SerializeField]
    private List<AudioClip> Sass; // Música de categoría Sass

    [SerializeField]
    private List<AudioClip> DefaultList; // Música de categoría Default

    [SerializeField]
    private AudioSource audioSource; // Componente AudioSource para reproducir

    private int currentTrackIndex = 0; // Índice de la canción actual

    [SerializeField]
    private GameObject dropdownObject; // GameObject del dropdown para seleccionar categoría

    [SerializeField]
    private GameObject sliderObject; // GameObject del slider para controlar volumen

    private TMP_Dropdown dropdown; // Referencia al componente TMP_Dropdown
    private Slider volumeSlider; // Referencia al componente Slider

    private bool playPauseCalled = false; // Indica si el usuario presionó pausa/play

    [SerializeField]
    private bool playMusicOnStart = true; // Si es true, reproduce música al iniciar

    private bool hasStartedPlayback = false; // Marca si ya comenzó la reproducción

    void Start()
    {
        // Inicializa referencias a componentes y carga música según configuración
        if (audioSource == null)
        {
            Debug.LogError("audioSource is null in Start method of MusicManagerScript");
            return;
        }

        if (dropdownObject != null)
        {
            dropdown = dropdownObject.GetComponent<TMP_Dropdown>();
        }
        else
        {
            Debug.LogError("dropdownObject is null in Start method of MusicManagerScript");
        }

        if (sliderObject != null)
        {
            volumeSlider = sliderObject.GetComponent<Slider>();
            volumeSlider.onValueChanged.AddListener(delegate { SetVolume(); });
        }
        else
        {
            Debug.LogError("sliderObject is null in Start method of MusicManagerScript");
        }
        LoadMusicClips();

        if (playMusicOnStart && musicClips.Count > 0)
        {
            PlayRandomMusic();
            hasStartedPlayback = true;
        }
        else
        {
            hasStartedPlayback = false;
        }

        StartCoroutine(CheckAudioStatus());
    }

    // Corutina que verifica el estado del audio y avanza a la siguiente canción cuando termina
    private IEnumerator CheckAudioStatus()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            // Only auto-advance if playback has actually started before
            if (hasStartedPlayback && !audioSource.isPlaying && musicClips.Count > 0 && !playPauseCalled)
            {
                NextSong();
            }
        }
    }


    // Reproduce la canción actual sin loop (modo lista normal)
    public void PlayMusic()
    {
        if (musicClips.Count > 0)
        {
            audioSource.loop = false;
            audioSource.clip = musicClips[currentTrackIndex];
            audioSource.Play();
            playPauseCalled = false;
            hasStartedPlayback = true; // <-- mark started
        }
        else Debug.LogError("musicClips is empty in PlayMusic");
    }

    // Selecciona una canción al azar y la reproduce
    public void PlayRandomMusic()
    {
        if (musicClips.Count > 0)
        {
            currentTrackIndex = Random.Range(0, musicClips.Count);
            PlayMusic();
            hasStartedPlayback = true; // redundant but explicit
        }
        else Debug.LogError("musicClips is empty in PlayRandomMusic");
    }

    // Pausa la canción actual
    public void PauseMusic()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Pause();
            playPauseCalled = true;
        }
        else
        {
            Debug.LogError("audioSource is not playing in PauseMusic method of MusicManagerScript");
        }
    }

    // Detiene la reproducción actual
    public void StopMusic()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
            playPauseCalled = true;
        }
        else
        {
            Debug.LogError("audioSource is not playing in StopMusic method of MusicManagerScript");
        }
    }

    // Avanza a la siguiente canción en la lista
    public void NextSong()
    {
        if (musicClips.Count > 0)
        {
            currentTrackIndex = (currentTrackIndex + 1) % musicClips.Count;
            PlayMusic();
        }
        else
        {
            Debug.LogError("musicClips is empty in NextSong method of MusicManagerScript");
        }
    }

    // Retrocede a la canción anterior en la lista
    public void PreviousSong()
    {
        if (musicClips.Count > 0)
        {
            currentTrackIndex = (currentTrackIndex - 1 + musicClips.Count) % musicClips.Count;
            PlayMusic();
        }
        else
        {
            Debug.LogError("musicClips is empty in PreviousSong method of MusicManagerScript");
        }
    }

    // Reproduce una canción específica por nombre (activa loop para esa canción)
    public void PlaySongByName(string songName)
    {
        // 1) Try current list first
        int songIndex = FindIndexByName(songName, musicClips);
        if (songIndex != -1)
        {
            currentTrackIndex = songIndex;
            audioSource.loop = true;                //  repeat this specific song
            audioSource.clip = musicClips[currentTrackIndex];
            audioSource.Play();
            playPauseCalled = false;
            return;
        }

        // 2) Fallback: search across all lists
        if (TryFindInAllLists(songName, out var clip, out var sourceList, out var idx))
        {
            audioSource.loop = true;
            audioSource.clip = clip;
            audioSource.Play();
            playPauseCalled = false;
            hasStartedPlayback = true;

            // (Optional) switch active list if you want NextSong to follow that bank
            // musicClips = new List<AudioClip>(sourceList);
            // currentTrackIndex = idx;
            return;
        }

        // 3) Not found anywhere: log and resume current track after a small delay
        Debug.LogError("songName not found in PlaySongByName: " + songName);
        StartCoroutine(ResumeCurrentTrackAfterDelay(1f)); // PlayMusic() will clear loop anyway
    }

    public void DisableLoop() => audioSource.loop = false;

    // Corutina que reanuda la canción actual después de un retraso
    private IEnumerator ResumeCurrentTrackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayMusic();
    }

    // Establece el volumen del audio según el valor del slider
    public void SetVolume()
    {
        if (volumeSlider != null)
        {
            audioSource.volume = Mathf.Clamp01(volumeSlider.value);
        }
        else
        {
            Debug.LogError("volumeSlider is null in SetVolume method of MusicManagerScript");
        }
    }

    // Carga la lista de música según la opción seleccionada en el dropdown
    public void LoadMusicClips()
    {
        string folderName = "Default";
        if (dropdown != null) folderName = dropdown.options[dropdown.value].text;
        else Debug.LogError("dropdown is null in LoadMusicClips method of MusicManagerScript");

        switch (folderName)
        {
            case "Accion": musicClips = new List<AudioClip>(Accion); break;
            case "Favoritas": musicClips = new List<AudioClip>(Favoritas); break;
            case "Relax": musicClips = new List<AudioClip>(Relax); break;
            case "Sass": musicClips = new List<AudioClip>(Sass); break;
            case "Default": musicClips = new List<AudioClip>(DefaultList); break;
            default:
                Debug.LogWarning("No matching music list found for: " + folderName);
                musicClips.Clear();
                break;
        }

        if (musicClips.Count == 0)
            Debug.LogWarning("No valid music files found in the specified list.");
    }

    // Alterna entre play y pausa según el estado actual de reproducción
    public void PlayPause()
    {
        playPauseCalled = true; // Set the flag to indicate PlayPause was called

        if (audioSource.isPlaying)
        {
            PauseMusic();
        }
        else
        {
            PlayMusic();
        }
    }

    // Búsqueda case-insensitive del índice de una canción por nombre en una lista
    private int FindIndexByName(string songName, List<AudioClip> list)
    {
        if (list == null) return -1;
        string target = (songName ?? "").Trim();
        for (int i = 0; i < list.Count; i++)
        {
            var clip = list[i];
            if (clip == null) continue;
            if (string.Equals(clip.name.Trim(), target, System.StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    // Busca en todas las listas de música y retorna el clip, la lista de origen e índice
    private bool TryFindInAllLists(string songName, out AudioClip clip, out List<AudioClip> sourceList, out int index)
    {
        var banks = new List<List<AudioClip>> { Accion, Favoritas, Relax, Sass, DefaultList, musicClips };
        foreach (var bank in banks)
        {
            int idx = FindIndexByName(songName, bank);
            if (idx != -1)
            {
                clip = bank[idx];
                sourceList = bank;
                index = idx;
                return true;
            }
        }
        clip = null;
        sourceList = null;
        index = -1;
        return false;
    }


}
