using System.Text;
using UnityEngine;
using TMPro;

/// <summary>
/// Displays runtime info about AudioSources in this prefab.
/// Searches the scene for a GameObject named "AudioScriptDebuggerTxt"
/// containing a TextMeshProUGUI to print the state of each AudioSource.
/// </summary>
public class AudioSourceDebugger : MonoBehaviour
{
    [Header("Audio Sources to Monitor")]
    [Tooltip("Assign AudioSources from this prefab.")]
    public AudioSource[] audioSources;

    [Header("Optional - Will auto-find by name if left null")]
    [Tooltip("Will automatically search for a TextMeshProUGUI named 'AudioScriptDebuggerTxt' in the scene.")]
    public TextMeshProUGUI debugText;

    [Header("Update Interval")]
    [Tooltip("How often to refresh the displayed info (seconds).")]
    public float refreshInterval = 0.25f;

    private float _timer;

    // at top of AudioSourceDebugger
    [SerializeField] private int maxUiChars = 5000;
    [SerializeField] private bool alsoLogFullToConsole = false;



    private void Start()
    {
        // Auto-find the debug text in the active scene
        if (debugText == null)
        {
            GameObject txtObj = GameObject.Find("AudioScriptDebuggerTxt");
            if (txtObj != null)
                debugText = txtObj.GetComponent<TextMeshProUGUI>();

            if (debugText == null)
                Debug.LogWarning("[AudioSourceDebugger] Could not find 'AudioScriptDebuggerTxt' in scene.");
        }
    }

    private void Update()
    {
        if (debugText == null || audioSources == null || audioSources.Length == 0)
            return;

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            _timer = refreshInterval;
            UpdateDebugText();
        }
    }

    private void UpdateDebugText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("<b><size=110%>Audio Source Debugger</size></b>");
        sb.AppendLine("----------------------------------");

        for (int i = 0; i < audioSources.Length; i++)
        {
            var src = audioSources[i];
            if (src == null)
            {
                sb.AppendLine($"[{i}] <color=red>Null reference</color>");
                continue;
            }

            string goState = src.gameObject.activeSelf ? "Active" : "<color=grey>Inactive</color>";
            string compState = src.enabled ? "Enabled" : "<color=grey>Disabled</color>";
            string clipName = src.clip ? src.clip.name : "<color=grey>No Clip</color>";
            string playState = src.isPlaying ? "<color=green>Playing</color>" : "<color=orange>Stopped</color>";

            sb.AppendLine(
                $"[{i}] <b>{src.gameObject.name}</b>\n" +
                $" • GO: {goState} | Component: {compState}\n" +
                $" • Clip: {clipName}\n" +
                $" • State: {playState}\n");
        }

        debugText.text = sb.ToString();

        string result = sb.ToString();

        // Optional: log full to Console (Unity Console itself truncates long lines ~16k chars,
        // but this is still helpful for inspection)
        if (alsoLogFullToConsole) Debug.Log(result);

        // Clamp for UI to avoid TMP vertex cap spikes and keep frames smooth
        if (maxUiChars > 0 && result.Length > maxUiChars)
        {
            result = result.Substring(0, maxUiChars)
                   + "\n<size=80%><color=grey>...[truncated]</color></size>";
        }

        debugText.text = result;

    }
}

