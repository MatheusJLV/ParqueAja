using UnityEngine;
using UnityEngine.UI;

// Controlador de partículas: gestiona reproducción, color, tamaño y efectos (ruido, colisiones).
public class ParticleController : MonoBehaviour
{
    public ParticleSystem particulas; // Asigna desde el Inspector o se obtiene en Start
    public Slider colorSlider;  // Control del color HSV
    public Image colorSliderFill; // Indicador visual del color seleccionado
    private Color currentColor; // Color actual de las partículas
    public Button startBtn;     // Botón de inicio
    public Button pauseBtn;     // Botón de pausa
    public Button stopBtn;      // Botón de parada
    public Button clearBtn;     // Botón de limpiar
    public Slider sizeSlider;   // Control del tamaño
    public Slider speedSlider;  // Control de velocidad

    public float strength;      // Fuerza del ruido
    public float frequency;     // Frecuencia del ruido
    public float scrollSpeed;   // Velocidad de desplazamiento del ruido

    public float dampen;        // Amortiguación en colisiones
    public float bounce;        // Rebote en colisiones
    public float lifeLoss;      // Pérdida de vida en colisiones

    public Slider StrengthSlider;       // Control de fuerza
    public Slider frequencySlider;      // Control de frecuencia
    public Slider scrollSpeedSlider;    // Control de velocidad de desplazamiento

    public Slider dampenSlider;         // Control de amortiguación
    public Slider bounceSlider;         // Control de rebote
    public Slider lifeLossSlider;       // Control de pérdida de vida

    public Button noiseBtn;     // Botón para habilitar ruido
    public Button collisionBtn; // Botón para habilitar colisiones

    // Configura referencias y conecta listeners de UI en la inicialización.
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
    // Establece el tamaño inicial de las partículas
    public void SetStartSize(float size)
    {
        var main = particulas.main;
        main.startSize = size;
    }

    // Establece la velocidad inicial de las partículas
    public void SetStartSpeed(float speed)
    {
        var main = particulas.main;
        main.startSpeed = speed;
    }

    // Establece el color inicial de las partículas
    public void SetStartColor(Color color)
    {
        var main = particulas.main;
        main.startColor = color;
    }

    // === MÓDULO DE RUIDO ===
    // Habilita el módulo de ruido con los parámetros configurados
    public void SetNoise()
    {
        var noise = particulas.noise;
        noise.enabled = true;
        noise.strength = strength;
        noise.frequency = frequency;
        noise.scrollSpeed = scrollSpeed;
    }

    // === MÓDULO DE COLISIONES ===
    // Habilita el módulo de colisiones con los parámetros configurados
    public void SetCollision()
    {
        var collision = particulas.collision;
        collision.enabled = true;
        collision.dampen = dampen;
        collision.bounce = bounce;
        collision.lifetimeLoss = lifeLoss;
    }
}
