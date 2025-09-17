using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MusicManagerScript : MonoBehaviour
{
    [SerializeField]
    private List<AudioClip> musicClips; // List of music clips

    [SerializeField]
    private List<AudioClip> Accion; // List of Accion music clips

    [SerializeField]
    private List<AudioClip> Favoritas; // List of Favoritas music clips

    [SerializeField]
    private List<AudioClip> Relax; // List of Relax music clips

    [SerializeField]
    private List<AudioClip> Sass; // List of Sass music clips

    [SerializeField]
    private List<AudioClip> DefaultList; // List of Default music clips

    [SerializeField]
    private AudioSource audioSource; // Reference to the AudioSource component

    private int currentTrackIndex = 0; // Index of the current track

    [SerializeField]
    private GameObject dropdownObject; // Reference to the dropdown GameObject

    [SerializeField]
    private GameObject sliderObject; // Reference to the slider GameObject

    private TMP_Dropdown dropdown; // Reference to the TMP_Dropdown component
    private Slider volumeSlider; // Reference to the Slider component

    private bool playPauseCalled = false; // Flag to indicate if PlayPause was called

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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

        LoadMusicClips(); // Load default music clips

        if (musicClips.Count > 0)
        {
            PlayRandomMusic();
        }

        // Start the coroutine to check the audio status
        StartCoroutine(CheckAudioStatus());
    }

    // Coroutine to check the audio status
    private IEnumerator CheckAudioStatus()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f); // Check every second

            if (!audioSource.isPlaying && musicClips.Count > 0 && !playPauseCalled)
            {
                NextSong();
            }
        }
    }

    // Play the current track
    public void PlayMusic()
    {
        if (musicClips.Count > 0)
        {
            audioSource.clip = musicClips[currentTrackIndex];
            audioSource.Play();
            playPauseCalled = false;
        }
        else
        {
            Debug.LogError("musicClips is empty in PlayMusic method of MusicManagerScript");
        }
    }

    // Play a random track
    public void PlayRandomMusic()
    {
        if (musicClips.Count > 0)
        {
            currentTrackIndex = Random.Range(0, musicClips.Count);
            PlayMusic();
        }
        else
        {
            Debug.LogError("musicClips is empty in PlayRandomMusic method of MusicManagerScript");
        }
    }

    // Pause the current track
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

    // Stop the current track
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

    // Play the next track in the list
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

    // Play the previous track in the list
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

    // Play a specific song by name
    public void PlaySongByName(string songName)
    {
        // 1) Try current list first
        int songIndex = FindIndexByName(songName, musicClips);
        if (songIndex != -1)
        {
            currentTrackIndex = songIndex;
            PlayMusic();
            return;
        }

        // 2) Fallback: search across all lists
        if (TryFindInAllLists(songName, out var clip, out var sourceList, out var idx))
        {
            // Option A: just play the clip immediately without changing the active list
            audioSource.clip = clip;
            audioSource.Play();
            playPauseCalled = false;

            // Option B (optional): switch active list so NextSong follows that bank
            // musicClips = new List<AudioClip>(sourceList);
            // currentTrackIndex = idx;

            return;
        }

        // 3) Not found anywhere: log and resume current track after a small delay
        Debug.LogError("songName not found in PlaySongByName: " + songName);
        StartCoroutine(ResumeCurrentTrackAfterDelay(1f));
    }


    // Coroutine to resume the current track after a delay
    private IEnumerator ResumeCurrentTrackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayMusic();
    }

    // Set the volume of the audio source based on the slider value
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

    // Load music clips from the specified list
    public void LoadMusicClips()
    {
        string folderName = "Default";
        if (dropdown != null)
        {
            folderName = dropdown.options[dropdown.value].text;
        }
        else
        {
            Debug.LogError("dropdown is null in LoadMusicClips method of MusicManagerScript");
        }

        switch (folderName)
        {
            case "Accion":
                musicClips = new List<AudioClip>(Accion);
                PlayRandomMusic();
                break;
            case "Favoritas":
                musicClips = new List<AudioClip>(Favoritas);
                PlayRandomMusic();
                break;
            case "Relax":
                musicClips = new List<AudioClip>(Relax);
                PlayRandomMusic();
                break;
            case "Sass":
                musicClips = new List<AudioClip>(Sass);
                PlayRandomMusic();
                break;
            case "Default":
                musicClips = new List<AudioClip>(DefaultList);
                PlayRandomMusic();
                break;
            default:
                Debug.LogWarning("No matching music list found for: " + folderName);
                musicClips.Clear();
                break;
        }

        if (musicClips.Count == 0)
        {
            Debug.LogWarning("No valid music files found in the specified list.");
        }
    }

    // Play or pause the music based on the current state
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

    // Case-insensitive, trimmed match against one list
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

    // Search all lists in order; return the clip and the list it came from
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
