using UnityEngine;
using UnityEngine.UI;

public class ParticleController : MonoBehaviour
{
    public ParticleSystem particulas; // Asigna desde el Inspector o se obtiene en Start
    public Slider colorSlider;
    public Image colorSliderFill;
    private Color currentColor; // Color inicial
    public Button startBtn;
    public Button pauseBtn;
    public Button stopBtn;
    public Button clearBtn;
    public Slider sizeSlider;
    public Slider speedSlider;

    public float strength;
    public float frequency;
    public float scrollSpeed;

    public float dampen;
    public float bounce;
    public float lifeLoss;

    public Slider StrengthSlider;
    public Slider frequencySlider;
    public Slider scrollSpeedSlider;

    public Slider dampenSlider;
    public Slider bounceSlider;
    public Slider lifeLossSlider;

    public Button noiseBtn;
    public Button collisionBtn;

    void Start()
    {
        // Si no se asignó desde el Inspector, intenta obtenerlo automáticamente
        if (particulas == null)
            particulas = GetComponent<ParticleSystem>();
        if (colorSlider != null)
            colorSlider.onValueChanged.AddListener(OnColorSliderChanged);
        if (startBtn != null)
            startBtn.onClick.AddListener(StartParticles);
        if (pauseBtn != null)
            pauseBtn.onClick.AddListener(PauseParticles);
        if (stopBtn != null)
            stopBtn.onClick.AddListener(StopParticles);
        if (startBtn != null)
            clearBtn.onClick.AddListener(ClearParticles);
        if (sizeSlider != null)
            sizeSlider.onValueChanged.AddListener(OnSizeChanged);
        if (speedSlider != null)
            speedSlider.onValueChanged.AddListener(OnSpeedChanged);

        if (StrengthSlider != null)
            StrengthSlider.onValueChanged.AddListener(OnStrengthChanged);
        if (frequencySlider != null)
            frequencySlider.onValueChanged.AddListener(OnFrequencyChanged);
        if (scrollSpeedSlider != null)
            scrollSpeedSlider.onValueChanged.AddListener(OnScrollSpeedChanged);
        if (dampenSlider != null)
            dampenSlider.onValueChanged.AddListener(OnDampenChanged);
        if (bounceSlider != null)
            bounceSlider.onValueChanged.AddListener(OnBounceChanged);
        if (lifeLossSlider != null)
            lifeLossSlider.onValueChanged.AddListener(OnLifeLossChanged);

        if (noiseBtn != null)
            noiseBtn.onClick.AddListener(SetNoise);
        if (collisionBtn != null)
            collisionBtn.onClick.AddListener(SetCollision);
    }
    void OnLifeLossChanged(float value)
    {
        lifeLoss = value;
    }
    void OnBounceChanged(float value)
    {
        bounce = value;
    }
    void OnDampenChanged(float value)
    {
        dampen = value;
    }
    void OnScrollSpeedChanged(float value)
    {
        scrollSpeed = value;
    }
    void OnFrequencyChanged(float value)
    {
        frequency = value;
    }
    void OnStrengthChanged(float value)
    {
        strength = value;
    }

    void OnColorSliderChanged(float value)
    {
        currentColor = Color.HSVToRGB(value, 1f, 1f);
        if (colorSliderFill != null)
            colorSliderFill.color = currentColor;
        SetStartColor(currentColor);
    }

    void OnSizeChanged(float value)
    {
        SetStartSize(value);
    }

    void OnSpeedChanged(float value)
    {
        SetStartSpeed(value);
    }

    // Inicia el sistema de partículas
    public void StartParticles()
    {
        if (particulas != null)
            particulas.Play();
    }

    // Detiene el sistema de partículas
    public void StopParticles()
    {
        if (particulas != null)
            particulas.Stop();
    }

    // Pausa el sistema de partículas
    public void PauseParticles()
    {
        if (particulas != null)
            particulas.Pause();
    }

    // Limpia todas las partículas activas
    public void ClearParticles()
    {
        if (particulas != null)
            particulas.Clear();
    }
    public void SetStartSize(float size)
    {
        var main = particulas.main;
        main.startSize = size;
    }

    public void SetStartSpeed(float speed)
    {
        var main = particulas.main;
        main.startSpeed = speed;
    }

    public void SetStartColor(Color color)
    {
        var main = particulas.main;
        main.startColor = color;
    }

    // === NOISE MODULE ===

    public void SetNoise()
    {
        var noise = particulas.noise;
        noise.enabled = true;
        noise.strength = strength;
        noise.frequency = frequency;
        noise.scrollSpeed = scrollSpeed;
    }

    // === COLLISION MODULE ===

    public void SetCollision()
    {
        var collision = particulas.collision;
        collision.enabled = true;
        collision.dampen = dampen;
        collision.bounce = bounce;
        collision.lifetimeLoss = lifeLoss;
    }
}
