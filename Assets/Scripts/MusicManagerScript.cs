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
        int songIndex = musicClips.FindIndex(clip => clip.name == songName);
        if (songIndex != -1)
        {
            currentTrackIndex = songIndex;
            PlayMusic();
        }
        else
        {
            Debug.LogError("songName not found in PlaySongByName method of MusicManagerScript");
            StartCoroutine(ResumeCurrentTrackAfterDelay(1f));
        }
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
}
