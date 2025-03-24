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

    private AudioSource audioSource; // AudioSource component
    private int currentTrackIndex = 0; // Index of the current track

    [SerializeField]
    private GameObject dropdownObject; // Reference to the dropdown GameObject

    [SerializeField]
    private GameObject sliderObject; // Reference to the slider GameObject



    private TMP_Dropdown dropdown; // Reference to the TMP_Dropdown component
    private Slider volumeSlider; // Reference to the Slider component
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (dropdownObject != null)
        {
            dropdown = dropdownObject.GetComponent<TMP_Dropdown>();
        }

        if (sliderObject != null)
        {
            volumeSlider = sliderObject.GetComponent<Slider>();
            volumeSlider.onValueChanged.AddListener(delegate { SetVolume(); });
        }

        

        LoadMusicClips(); // Load default music clips

        if (musicClips.Count > 0)
        {
            PlayMusic();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Automatically play the next song when the current one finishes
        if (!audioSource.isPlaying && musicClips.Count > 0)
        {
            NextSong();
        }
    }

    // Play the current track
    public void PlayMusic()
    {
        if (musicClips.Count > 0)
        {
            audioSource.clip = musicClips[currentTrackIndex];
            audioSource.Play();
        }
    }

    // Pause the current track
    public void PauseMusic()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }

    // Stop the current track
    public void StopMusic()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
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
    }

    // Play the previous track in the list
    public void PreviousSong()
    {
        if (musicClips.Count > 0)
        {
            currentTrackIndex = (currentTrackIndex - 1 + musicClips.Count) % musicClips.Count;
            PlayMusic();
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
    }

    // Load music clips from the specified folder
    public void LoadMusicClips()
    {
        string folderName = "Default";
        if (dropdown != null)
        {
            folderName = dropdown.options[dropdown.value].text;
        }

        string path = Path.Combine(Application.dataPath, "Music", folderName);
        if (Directory.Exists(path))
        {
            string[] files = Directory.GetFiles(path, "*.mp3");
            if (files.Length > 0)
            {
                musicClips.Clear();
                foreach (string file in files)
                {
                    string relativePath = "Music/" + folderName + "/" + Path.GetFileNameWithoutExtension(file);
                    AudioClip clip = Resources.Load<AudioClip>(relativePath);
                    if (clip != null)
                    {
                        musicClips.Add(clip);
                    }
                }

                if (musicClips.Count == 0)
                {
                    Debug.LogWarning("No valid music files found in the specified folder.");
                }
            }
            else
            {
                Debug.LogWarning("No music files found in the specified folder.");
            }
        }
        else
        {
            Debug.LogError("The specified folder does not exist: " + path);
        }
    }

    // Set the active state of the objects in the list based on the toggle value
    
}
