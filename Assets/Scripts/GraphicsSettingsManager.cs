using UnityEngine;
using UnityStandardAssets.ImageEffects;

public enum GraphicsMode
{
    Fast, Fancy, Insane
}

public class GraphicsSettingsManager : MonoBehaviour
{
    public static GraphicsSettingsManager Instance;

    [SerializeField] private CameraMotionBlur motionBlurEffect;
    [SerializeField] private BloomAndFlares bloomEffect;
    [SerializeField] private ScreenSpaceAmbientOcclusion occlusionEffect;
    [SerializeField] private Light sun;

    [SerializeField] private Material epicImageEffectsChunkMat;
    [SerializeField] private Material standardChunkMat;
    [SerializeField] private Material fastChunkMat;

    public GraphicsMode gMode = GraphicsMode.Fancy;

    void Awake()
    {
        Instance = this;
    }

    public void ChangeGraphicsMode()
    {
        if (gMode == GraphicsMode.Fast) gMode = GraphicsMode.Fancy;
        else if (gMode == GraphicsMode.Fancy) gMode = GraphicsMode.Insane;
        else gMode = GraphicsMode.Fast;

        ApplyGraphicsSettings();
    }

    public void ApplyGraphicsSettings()
    {
        bool enableHighQualityEffects = gMode == GraphicsMode.Insane;
        bloomEffect.enabled = enableHighQualityEffects;
        motionBlurEffect.enabled = enableHighQualityEffects;
        occlusionEffect.enabled = enableHighQualityEffects;
        RenderSettings.fog = enableHighQualityEffects;
        sun.shadows = enableHighQualityEffects ? LightShadows.Soft : LightShadows.None;
    }

    public Material GetChunkMaterial() => gMode switch
    {
        GraphicsMode.Fast => fastChunkMat,
        GraphicsMode.Fancy => standardChunkMat,
        GraphicsMode.Insane => epicImageEffectsChunkMat,
        _ => standardChunkMat,
    };
}
