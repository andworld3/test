using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[System.Serializable]
public struct ALPreset
{
    public string name;
    public float masterGain;
    public float alGain;
    public float alSmooth;
    public float emissionGain;
    public bool safeMode;
    public bool beatPulse;
}

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class ALPresetController : UdonSharpBehaviour
{
    [Header("Target Renderers")]
    [SerializeField] private Renderer[] targetRenderers;

    [Header("Presets")]
    [SerializeField] private ALPreset calmPreset = new ALPreset
    {
        name = "Calm",
        masterGain = 0.7f,
        alGain = 1.0f,
        alSmooth = 0.25f,
        emissionGain = 0.8f,
        safeMode = true,
        beatPulse = false
    };

    [SerializeField] private ALPreset defaultPreset = new ALPreset
    {
        name = "Default",
        masterGain = 1.0f,
        alGain = 1.5f,
        alSmooth = 0.15f,
        emissionGain = 1.0f,
        safeMode = false,
        beatPulse = true
    };

    [SerializeField] private ALPreset hypePreset = new ALPreset
    {
        name = "Hype",
        masterGain = 1.3f,
        alGain = 2.0f,
        alSmooth = 0.08f,
        emissionGain = 1.5f,
        safeMode = false,
        beatPulse = true
    };

    [Header("Current Settings")]
    [UdonSynced] public int currentPresetIndex = 1; // Default
    [UdonSynced] public float masterGain = 1.0f;
    [UdonSynced] public float emissionGain = 1.0f;
    [UdonSynced] public float smoothing = 0.15f;
    [UdonSynced] public bool safeMode = false;
    [UdonSynced] public bool beatPulse = true;

    [Header("Runtime Settings")]
    [SerializeField] private float updateInterval = 0.1f; // Update every 100ms
    [SerializeField] private bool debugOutput = false;

    // Private variables
    private MaterialPropertyBlock[] materialPropertyBlocks;
    private ALPreset[] presets;
    private float lastUpdateTime;
    private bool needsUpdate = true;

    // Property IDs (cached for performance)
    private int _AL_Gain_ID;
    private int _AL_Smooth_ID;
    private int _EmissionGain_ID;
    private int _AL_Enable_ID;

    void Start()
    {
        // Initialize presets array
        presets = new ALPreset[] { calmPreset, defaultPreset, hypePreset };

        // Cache property IDs
        _AL_Gain_ID = Shader.PropertyToID("_AL_Gain");
        _AL_Smooth_ID = Shader.PropertyToID("_AL_Smooth");
        _EmissionGain_ID = Shader.PropertyToID("_EmissionGain");
        _AL_Enable_ID = Shader.PropertyToID("_AL_Enable");

        // Initialize MaterialPropertyBlocks
        InitializeMaterialPropertyBlocks();

        // Apply current preset
        ApplyCurrentPreset();

        if (debugOutput)
            Debug.Log($"[ALPresetController] Initialized with {targetRenderers.Length} renderers");
    }

    void Update()
    {
        // Throttle updates to improve performance
        if (Time.time - lastUpdateTime < updateInterval && !needsUpdate)
            return;

        if (needsUpdate)
        {
            ApplyMaterialProperties();
            needsUpdate = false;
            lastUpdateTime = Time.time;
        }
    }

    private void InitializeMaterialPropertyBlocks()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            Debug.LogWarning("[ALPresetController] No target renderers assigned!");
            return;
        }

        materialPropertyBlocks = new MaterialPropertyBlock[targetRenderers.Length];
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            materialPropertyBlocks[i] = new MaterialPropertyBlock();
            if (targetRenderers[i] != null)
                targetRenderers[i].GetPropertyBlock(materialPropertyBlocks[i]);
        }
    }

    public void SetPreset(int presetIndex)
    {
        presetIndex = Mathf.Clamp(presetIndex, 0, presets.Length - 1);

        if (currentPresetIndex != presetIndex)
        {
            currentPresetIndex = presetIndex;
            ApplyCurrentPreset();

            if (Networking.IsOwner(gameObject))
                RequestSerialization();

            if (debugOutput)
                Debug.Log($"[ALPresetController] Switched to preset: {presets[presetIndex].name}");
        }
    }

    public void SetMasterGain(float value)
    {
        float clampedValue = Mathf.Clamp(value, 0.0f, 2.0f);
        if (Mathf.Abs(masterGain - clampedValue) > 0.01f)
        {
            masterGain = clampedValue;
            needsUpdate = true;

            if (Networking.IsOwner(gameObject))
                RequestSerialization();
        }
    }

    public void SetEmissionGain(float value)
    {
        float clampedValue = Mathf.Clamp(value, 0.0f, 3.0f);
        if (Mathf.Abs(emissionGain - clampedValue) > 0.01f)
        {
            emissionGain = clampedValue;
            needsUpdate = true;

            if (Networking.IsOwner(gameObject))
                RequestSerialization();
        }
    }

    public void SetSmoothing(float value)
    {
        float clampedValue = Mathf.Clamp(value, 0.05f, 0.5f);
        if (Mathf.Abs(smoothing - clampedValue) > 0.01f)
        {
            smoothing = clampedValue;
            needsUpdate = true;

            if (Networking.IsOwner(gameObject))
                RequestSerialization();
        }
    }

    public void SetSafeMode(bool enabled)
    {
        if (safeMode != enabled)
        {
            safeMode = enabled;
            needsUpdate = true;

            if (Networking.IsOwner(gameObject))
                RequestSerialization();

            if (debugOutput)
                Debug.Log($"[ALPresetController] Safe Mode: {enabled}");
        }
    }

    public void SetBeatPulse(bool enabled)
    {
        if (beatPulse != enabled)
        {
            beatPulse = enabled;
            needsUpdate = true;

            if (Networking.IsOwner(gameObject))
                RequestSerialization();

            if (debugOutput)
                Debug.Log($"[ALPresetController] Beat Pulse: {enabled}");
        }
    }

    private void ApplyCurrentPreset()
    {
        if (presets == null || currentPresetIndex < 0 || currentPresetIndex >= presets.Length)
            return;

        ALPreset preset = presets[currentPresetIndex];

        masterGain = preset.masterGain;
        emissionGain = preset.emissionGain;
        smoothing = preset.alSmooth;
        safeMode = preset.safeMode;
        beatPulse = preset.beatPulse;

        needsUpdate = true;
    }

    private void ApplyMaterialProperties()
    {
        if (materialPropertyBlocks == null || targetRenderers == null)
            return;

        // Calculate final values
        float finalALGain = safeMode ?
            Mathf.Clamp(masterGain * 1.2f, 0.5f, 1.8f) :
            masterGain * 1.5f;

        float finalEmissionGain = safeMode ?
            Mathf.Clamp(emissionGain * 0.8f, 0.3f, 1.2f) :
            emissionGain;

        float finalSmoothing = safeMode ?
            Mathf.Clamp(smoothing + 0.1f, 0.15f, 0.4f) :
            smoothing;

        // Apply to all material property blocks
        for (int i = 0; i < materialPropertyBlocks.Length; i++)
        {
            if (materialPropertyBlocks[i] != null && targetRenderers[i] != null)
            {
                materialPropertyBlocks[i].SetFloat(_AL_Gain_ID, finalALGain);
                materialPropertyBlocks[i].SetFloat(_AL_Smooth_ID, finalSmoothing);
                materialPropertyBlocks[i].SetFloat(_EmissionGain_ID, finalEmissionGain);
                materialPropertyBlocks[i].SetFloat(_AL_Enable_ID, 1.0f);

                targetRenderers[i].SetPropertyBlock(materialPropertyBlocks[i]);
            }
        }

        if (debugOutput)
        {
            Debug.Log($"[ALPresetController] Applied: ALGain={finalALGain:F2}, " +
                     $"EmissionGain={finalEmissionGain:F2}, Smoothing={finalSmoothing:F2}, " +
                     $"SafeMode={safeMode}");
        }
    }

    public override void OnDeserialization()
    {
        needsUpdate = true;
    }

    public string GetCurrentPresetName()
    {
        if (presets != null && currentPresetIndex >= 0 && currentPresetIndex < presets.Length)
            return presets[currentPresetIndex].name;
        return "Unknown";
    }

    public ALPreset GetCurrentPreset()
    {
        if (presets != null && currentPresetIndex >= 0 && currentPresetIndex < presets.Length)
            return presets[currentPresetIndex];
        return defaultPreset;
    }

    // Public methods for UI integration
    public void NextPreset()
    {
        SetPreset((currentPresetIndex + 1) % presets.Length);
    }

    public void PreviousPreset()
    {
        SetPreset((currentPresetIndex - 1 + presets.Length) % presets.Length);
    }

    public void ToggleSafeMode()
    {
        SetSafeMode(!safeMode);
    }

    public void ToggleBeatPulse()
    {
        SetBeatPulse(!beatPulse);
    }
}