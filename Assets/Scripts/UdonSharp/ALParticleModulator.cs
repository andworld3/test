using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ALParticleModulator : UdonSharpBehaviour
{
    [Header("Target Particle Systems")]
    [SerializeField] private ParticleSystem[] particleSystems;

    [Header("Audio Data Settings")]
    [SerializeField] private bool useAudioData = false; // Audio Data (experimental) OFF by default
    [SerializeField] private AudioSource audioSource;

    [Header("Modulation Settings")]
    [SerializeField] private float baseEmissionRate = 10.0f;
    [SerializeField] private float emissionRateGain = 20.0f;
    [SerializeField] private float baseStartSpeed = 1.0f;
    [SerializeField] private float startSpeedGain = 2.0f;
    [SerializeField] private float baseStartSize = 1.0f;
    [SerializeField] private float startSizeGain = 0.5f;

    [Header("Smoothing & Safety")]
    [SerializeField] private float emaCoefficient = 0.15f; // EMA smoothing coefficient
    [SerializeField] private float idleLevel = 0.1f; // Minimum activity level
    [SerializeField] private float maxLevel = 1.0f; // Maximum activity level
    [SerializeField] private bool debugOutput = false;

    [Header("Band Selection")]
    [SerializeField] private int emissionBand = 0; // Bass for emission
    [SerializeField] private int speedBand = 2; // HighMid for speed
    [SerializeField] private int sizeBand = 0; // Bass for size

    // Private variables
    private ParticleSystem.EmissionModule[] emissionModules;
    private ParticleSystem.MainModule[] mainModules;
    private ParticleSystem.NoiseModule[] noiseModules;

    // Audio data processing
    private float[] audioSamples;
    private float[] smoothedLevels;
    private float lastUpdateTime;
    private float updateInterval = 0.05f; // Update 20 times per second

    // EMA smoothing
    private float smoothedEmissionLevel = 0.0f;
    private float smoothedSpeedLevel = 0.0f;
    private float smoothedSizeLevel = 0.0f;

    void Start()
    {
        InitializeParticleSystems();
        InitializeAudioProcessing();

        if (debugOutput)
            Debug.Log($"[ALParticleModulator] Initialized with {particleSystems.Length} particle systems");
    }

    void Update()
    {
        // Throttle updates for performance
        if (Time.time - lastUpdateTime < updateInterval)
            return;

        ProcessAudioAndUpdateParticles();
        lastUpdateTime = Time.time;
    }

    private void InitializeParticleSystems()
    {
        if (particleSystems == null || particleSystems.Length == 0)
        {
            Debug.LogWarning("[ALParticleModulator] No particle systems assigned!");
            return;
        }

        // Cache particle system modules for performance
        emissionModules = new ParticleSystem.EmissionModule[particleSystems.Length];
        mainModules = new ParticleSystem.MainModule[particleSystems.Length];
        noiseModules = new ParticleSystem.NoiseModule[particleSystems.Length];

        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] != null)
            {
                emissionModules[i] = particleSystems[i].emission;
                mainModules[i] = particleSystems[i].main;
                noiseModules[i] = particleSystems[i].noise;
            }
        }
    }

    private void InitializeAudioProcessing()
    {
        // Initialize audio sample array
        audioSamples = new float[256]; // Standard FFT size for VRChat
        smoothedLevels = new float[4]; // 4 bands: Bass, LowMid, HighMid, High

        // Try to find AudioSource if not assigned
        if (audioSource == null)
        {
            audioSource = FindObjectOfType<AudioSource>();
            if (audioSource == null && debugOutput)
                Debug.LogWarning("[ALParticleModulator] No AudioSource found. Using idle mode.");
        }
    }

    private void ProcessAudioAndUpdateParticles()
    {
        float emissionLevel, speedLevel, sizeLevel;

        if (useAudioData && audioSource != null && audioSource.isPlaying)
        {
            // Use Audio Data (experimental) - CPU-based audio analysis
            ProcessAudioData();
            emissionLevel = smoothedLevels[emissionBand];
            speedLevel = smoothedLevels[speedBand];
            sizeLevel = smoothedLevels[sizeBand];
        }
        else
        {
            // Fallback: Use gentle idle animation
            emissionLevel = GetIdleLevel(0.5f);
            speedLevel = GetIdleLevel(0.7f);
            sizeLevel = GetIdleLevel(0.3f);
        }

        // Apply EMA smoothing
        smoothedEmissionLevel = Mathf.Lerp(smoothedEmissionLevel, emissionLevel, emaCoefficient);
        smoothedSpeedLevel = Mathf.Lerp(smoothedSpeedLevel, speedLevel, emaCoefficient);
        smoothedSizeLevel = Mathf.Lerp(smoothedSizeLevel, sizeLevel, emaCoefficient);

        // Clamp values to safe ranges
        smoothedEmissionLevel = Mathf.Clamp(smoothedEmissionLevel, idleLevel, maxLevel);
        smoothedSpeedLevel = Mathf.Clamp(smoothedSpeedLevel, idleLevel, maxLevel);
        smoothedSizeLevel = Mathf.Clamp(smoothedSizeLevel, idleLevel, maxLevel);

        // Apply to particle systems
        UpdateParticleSystemProperties();

        if (debugOutput && Time.frameCount % 60 == 0) // Log every 60 frames
        {
            Debug.Log($"[ALParticleModulator] Levels - Emission: {smoothedEmissionLevel:F2}, " +
                     $"Speed: {smoothedSpeedLevel:F2}, Size: {smoothedSizeLevel:F2}");
        }
    }

    private void ProcessAudioData()
    {
        if (audioSource == null) return;

        // Get audio spectrum data
        audioSource.GetSpectrumData(audioSamples, 0, FFTWindow.BlackmanHarris);

        // Calculate band levels (simplified frequency band mapping)
        float bassLevel = CalculateBandLevel(0, 16); // ~0-86Hz
        float lowMidLevel = CalculateBandLevel(16, 64); // ~86-344Hz
        float highMidLevel = CalculateBandLevel(64, 128); // ~344-688Hz
        float highLevel = CalculateBandLevel(128, 256); // ~688-1375Hz

        // Normalize and apply to smoothed levels array
        smoothedLevels[0] = NormalizeAudioLevel(bassLevel);
        smoothedLevels[1] = NormalizeAudioLevel(lowMidLevel);
        smoothedLevels[2] = NormalizeAudioLevel(highMidLevel);
        smoothedLevels[3] = NormalizeAudioLevel(highLevel);
    }

    private float CalculateBandLevel(int startIndex, int endIndex)
    {
        float sum = 0.0f;
        int count = endIndex - startIndex;

        for (int i = startIndex; i < endIndex && i < audioSamples.Length; i++)
        {
            sum += audioSamples[i];
        }

        return count > 0 ? sum / count : 0.0f;
    }

    private float NormalizeAudioLevel(float rawLevel)
    {
        // Apply gain and normalization
        float normalized = rawLevel * 100.0f; // Amplify the typically small FFT values
        return Mathf.Clamp01(normalized);
    }

    private float GetIdleLevel(float basePhase)
    {
        // Generate gentle idle animation using sine waves
        float time = Time.time;
        float primary = Mathf.Sin(time * 0.5f + basePhase) * 0.5f + 0.5f;
        float secondary = Mathf.Sin(time * 0.3f + basePhase * 2.0f) * 0.3f + 0.3f;

        return Mathf.Lerp(idleLevel, idleLevel + 0.2f, primary * secondary);
    }

    private void UpdateParticleSystemProperties()
    {
        for (int i = 0; i < particleSystems.Length; i++)
        {
            if (particleSystems[i] == null) continue;

            // Update emission rate
            var emission = emissionModules[i];
            float newEmissionRate = baseEmissionRate + (smoothedEmissionLevel * emissionRateGain);
            emission.rateOverTime = newEmissionRate;

            // Update start speed
            var main = mainModules[i];
            float newStartSpeed = baseStartSpeed + (smoothedSpeedLevel * startSpeedGain);
            main.startSpeed = newStartSpeed;

            // Update start size
            float newStartSize = baseStartSize + (smoothedSizeLevel * startSizeGain);
            main.startSize = newStartSize;

            // Optional: Update noise strength if available
            if (noiseModules[i].enabled)
            {
                var noise = noiseModules[i];
                noise.strength = smoothedSpeedLevel * 0.5f;
            }
        }
    }

    // Public methods for external control
    public void SetUseAudioData(bool enabled)
    {
        useAudioData = enabled;
        if (debugOutput)
            Debug.Log($"[ALParticleModulator] Audio Data mode: {enabled}");
    }

    public void SetEMACoefficient(float coefficient)
    {
        emaCoefficient = Mathf.Clamp(coefficient, 0.05f, 0.5f);
    }

    public void SetBaseEmissionRate(float rate)
    {
        baseEmissionRate = Mathf.Max(0.0f, rate);
    }

    public void SetEmissionRateGain(float gain)
    {
        emissionRateGain = Mathf.Clamp(gain, 0.0f, 100.0f);
    }

    public void SetBaseStartSpeed(float speed)
    {
        baseStartSpeed = Mathf.Max(0.0f, speed);
    }

    public void SetStartSpeedGain(float gain)
    {
        startSpeedGain = Mathf.Clamp(gain, 0.0f, 10.0f);
    }

    public void SetBaseStartSize(float size)
    {
        baseStartSize = Mathf.Max(0.1f, size);
    }

    public void SetStartSizeGain(float gain)
    {
        startSizeGain = Mathf.Clamp(gain, 0.0f, 5.0f);
    }

    // Reset to idle state
    public void ResetToIdle()
    {
        smoothedEmissionLevel = idleLevel;
        smoothedSpeedLevel = idleLevel;
        smoothedSizeLevel = idleLevel;

        UpdateParticleSystemProperties();

        if (debugOutput)
            Debug.Log("[ALParticleModulator] Reset to idle state");
    }

    // Get current audio levels (for UI or debugging)
    public float GetEmissionLevel() { return smoothedEmissionLevel; }
    public float GetSpeedLevel() { return smoothedSpeedLevel; }
    public float GetSizeLevel() { return smoothedSizeLevel; }

    public bool IsUsingAudioData() { return useAudioData; }
    public bool IsAudioPlaying() { return audioSource != null && audioSource.isPlaying; }
}