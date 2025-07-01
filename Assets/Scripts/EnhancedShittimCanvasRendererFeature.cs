using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable]
public class ShittimCanvasSettings
{
    [Header("ShittimCanvas Texture")]
    [SerializeField] public Texture2D ShittimCanvasTexture;

    [Header("Fallback Options")]
    [SerializeField] private string textureResourcePath = ""; // Resources·��
    [SerializeField] private string textureGUID = ""; // �����GUID�����ڱ༭��

    // ����ʱ�������
    public Texture2D GetTexture()
    {
        // ���ȳ���ֱ������
        if (ShittimCanvasTexture != null)
        {
            return ShittimCanvasTexture;
        }

        // ���Դ�Resources����
        if (!string.IsNullOrEmpty(textureResourcePath))
        {
            Texture2D resourceTexture = Resources.Load<Texture2D>(textureResourcePath);
            if (resourceTexture != null)
            {
                //Debug.Log($"[ShittimCanvas] ��Resources��������: {textureResourcePath}");
                ShittimCanvasTexture = resourceTexture;
                return resourceTexture;
            }
        }

#if UNITY_EDITOR
        // �༭���г���ͨ��GUID����
        if (!string.IsNullOrEmpty(textureGUID))
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(textureGUID);
            if (!string.IsNullOrEmpty(assetPath))
            {
                Texture2D guidTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (guidTexture != null)
                {
                    //Debug.Log($"[ShittimCanvas] ͨ��GUID��������: {assetPath}");
                    ShittimCanvasTexture = guidTexture;
                    return guidTexture;
                }
            }
        }
#endif

        // ����Ĭ������
        return CreateDefaultTexture();
    }

    private Texture2D CreateDefaultTexture()
    {
        //Debug.LogWarning("[ShittimCanvas] ����Ĭ��ˮӡ����");

        Texture2D defaultTexture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[256 * 256];

        // ����һ���򵥵�Ĭ��ˮӡͼ��
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(128, 128));
                float alpha = Mathf.Clamp01(1.0f - distance / 100.0f);
                pixels[y * 256 + x] = new Color(1, 1, 1, alpha * 0.1f);
            }
        }

        defaultTexture.SetPixels(pixels);
        defaultTexture.Apply();
        defaultTexture.name = "DefaultShittimCanvas";

        return defaultTexture;
    }

#if UNITY_EDITOR
    // �༭�����Զ���¼������Ϣ
    public void RecordTextureInfo()
    {
        if (ShittimCanvasTexture != null)
        {
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(ShittimCanvasTexture);
            textureGUID = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);

            // ����Ƿ���Resources�ļ�����
            if (assetPath.Contains("/Resources/"))
            {
                int resourcesIndex = assetPath.LastIndexOf("/Resources/") + "/Resources/".Length;
                string resourcePath = assetPath.Substring(resourcesIndex);
                // �Ƴ��ļ���չ��
                int dotIndex = resourcePath.LastIndexOf('.');
                if (dotIndex > 0)
                {
                    resourcePath = resourcePath.Substring(0, dotIndex);
                }
                textureResourcePath = resourcePath;
                //Debug.Log($"[ShittimCanvas] ��¼Resources·��: {textureResourcePath}");
            }
        }
    }
#endif

    [Header("Render Timing")]
    public ShittimCanvasRenderEvent renderEvent = ShittimCanvasRenderEvent.BeforePostProcessing;

    [Header("Position Settings")]
    [Range(0f, 1f)]
    public float positionX = 0.9f;
    [Range(0f, 1f)]
    public float positionY = 0.1f;

    [Header("Size Settings")]
    [Range(0.01f, 1f)]
    public float scale = 0.2f;

    [Header("Opacity Settings")]
    [Range(0f, 1f)]
    public float opacity = 0.05f;

    [Header("Blend Mode")]
    public BlendMode blendMode = BlendMode.Overlay;

    [Header("Tiling")]
    public bool enableTiling = false;
    [Range(1, 10)]
    public int tilingCount = 3;

    [Header("Post-Processing Integration")]
    public bool integrateWithPostProcessing = true;
}

public enum BlendMode
{
    Alpha,
    Additive,
    Multiply,
    Overlay,
    Screen
}

public enum ShittimCanvasRenderEvent
{
    BeforePostProcessing,
    AfterPostProcessing,
    BeforeTransparents,
    AfterSkybox,
    AfterOpaques
}

public class EnhancedShittimCanvasRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private ShittimCanvasSettings settings = new ShittimCanvasSettings();
    [SerializeField] private Shader ShittimCanvasShader;

    private EnhancedShittimCanvasRenderPass ShittimCanvasPass;
    private Material ShittimCanvasMaterial;
    private Texture2D cachedTexture; // ������������

    // �����Shader���ƣ���������ʱ����
    private const string SHADER_NAME = "UI/ShittimCanvas";

    public override void Create()
    {
        //Debug.Log("[ShittimCanvas] Create method called");

        // ȷ����Create�׶δ������ʺ�Pass
        InitializeMaterial();
        InitializeRenderPass();

        // Ԥ��������
        PreloadTexture();
    }

    private void PreloadTexture()
    {
        cachedTexture = settings.GetTexture();
        if (cachedTexture != null)
        {
            //Debug.Log($"[ShittimCanvas] Ԥ��������ɹ�: {cachedTexture.name}");
        }
        else
        {
            //Debug.LogWarning("[ShittimCanvas] Ԥ��������ʧ��");
        }
    }

    private void InitializeMaterial()
    {
        // ����ʹ��ָ����Shader����
        if (ShittimCanvasShader == null)
        {
            ShittimCanvasShader = Shader.Find(SHADER_NAME);
        }

        // ����Ҳ���Shader��ʹ�ñ���Shader
        if (ShittimCanvasShader == null)
        {
            //Debug.LogWarning($"[ShittimCanvas] �޷��ҵ�Shader '{SHADER_NAME}'��ʹ��Ĭ��Sprite Shader");
            ShittimCanvasShader = Shader.Find("Sprites/Default");
        }

        // ��������
        if (ShittimCanvasShader != null)
        {
            if (ShittimCanvasMaterial != null)
            {
                CoreUtils.Destroy(ShittimCanvasMaterial);
            }
            ShittimCanvasMaterial = CoreUtils.CreateEngineMaterial(ShittimCanvasShader);
        }
    }

    private void InitializeRenderPass()
    {
        if (ShittimCanvasMaterial != null)
        {
            ShittimCanvasPass = new EnhancedShittimCanvasRenderPass(ShittimCanvasMaterial, settings);
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // ����ʱ��֤���ؽ�
        if (!ValidateAndRebuildIfNeeded())
        {
            return;
        }

        // ��֤����ʹ��GetTexture������
        Texture2D currentTexture = settings.GetTexture();
        if (currentTexture == null)
        {
            //Debug.LogWarning("[ShittimCanvas] �޷���ȡˮӡ����������Ⱦ");
            return;
        }

        // ���»��������
        if (cachedTexture != currentTexture)
        {
            cachedTexture = currentTexture;
            //Debug.Log($"[ShittimCanvas] ����������: {cachedTexture.name}");
        }

        // ֻ��Game��SceneView�������Ⱦ
        if (renderingData.cameraData.cameraType != CameraType.Game &&
            renderingData.cameraData.cameraType != CameraType.SceneView)
        {
            return;
        }

        //Debug.Log("[ShittimCanvas] �����ȾPass");

        // ������ȾPass
        RenderPassEvent targetEvent = GetRenderPassEvent(settings.renderEvent);
        ShittimCanvasPass.renderPassEvent = targetEvent;
        ShittimCanvasPass.ConfigureInput(ScriptableRenderPassInput.Color);

        // ����Pass��������Ⱦ����
        ShittimCanvasPass.Setup(renderer.cameraColorTarget, renderingData.cameraData.renderer);
        renderer.EnqueuePass(ShittimCanvasPass);
    }

    private bool ValidateAndRebuildIfNeeded()
    {
        bool needsRebuild = false;

        // ���Shader
        if (ShittimCanvasShader == null)
        {
            //Debug.LogWarning("[ShittimCanvas] Shader��ʧ���������²���");
            ShittimCanvasShader = Shader.Find(SHADER_NAME);
            needsRebuild = true;
        }

        // ���Material
        if (ShittimCanvasMaterial == null && ShittimCanvasShader != null)
        {
            //Debug.LogWarning("[ShittimCanvas] Material��ʧ�����´���");
            ShittimCanvasMaterial = CoreUtils.CreateEngineMaterial(ShittimCanvasShader);
            needsRebuild = true;
        }

        // ���RenderPass
        if (ShittimCanvasPass == null && ShittimCanvasMaterial != null)
        {
            //Debug.LogWarning("[ShittimCanvas] RenderPass��ʧ�����´���");
            ShittimCanvasPass = new EnhancedShittimCanvasRenderPass(ShittimCanvasMaterial, settings);
            needsRebuild = true;
        }

        // ������֤
        if (ShittimCanvasShader == null)
        {
            //Debug.LogError("[ShittimCanvas] �޷�����Shader");
            return false;
        }

        if (ShittimCanvasMaterial == null)
        {
            //Debug.LogError("[ShittimCanvas] �޷�����Material");
            return false;
        }

        if (ShittimCanvasPass == null)
        {
            //Debug.LogError("[ShittimCanvas] �޷�����RenderPass");
            return false;
        }

        if (needsRebuild)
        {
            //Debug.Log("[ShittimCanvas] ����ؽ����");
        }

        return true;
    }

    private RenderPassEvent GetRenderPassEvent(ShittimCanvasRenderEvent ShittimCanvasEvent)
    {
        switch (ShittimCanvasEvent)
        {
            case ShittimCanvasRenderEvent.BeforePostProcessing:
                return RenderPassEvent.BeforeRenderingPostProcessing;
            case ShittimCanvasRenderEvent.AfterPostProcessing:
                return RenderPassEvent.AfterRenderingPostProcessing;
            case ShittimCanvasRenderEvent.BeforeTransparents:
                return RenderPassEvent.BeforeRenderingTransparents;
            case ShittimCanvasRenderEvent.AfterSkybox:
                return RenderPassEvent.AfterRenderingSkybox;
            case ShittimCanvasRenderEvent.AfterOpaques:
                return RenderPassEvent.AfterRenderingOpaques;
            default:
                return RenderPassEvent.BeforeRenderingPostProcessing;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (ShittimCanvasMaterial != null)
            {
                CoreUtils.Destroy(ShittimCanvasMaterial);
                ShittimCanvasMaterial = null;
            }
        }
    }

    // �ڱ༭����֧��������
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (settings != null)
        {
            settings.RecordTextureInfo();
        }

        if (ShittimCanvasMaterial != null && ShittimCanvasShader != null && ShittimCanvasMaterial.shader != ShittimCanvasShader)
        {
            InitializeMaterial();
            InitializeRenderPass();
        }
    }
#endif
}

public class EnhancedShittimCanvasRenderPass : ScriptableRenderPass
{
    private Material material;
    private ShittimCanvasSettings settings;
    private RenderTargetIdentifier colorTarget;
    private ScriptableRenderer renderer;

    // ʹ��RTHandle���RenderTargetHandle (URP 12+�Ƽ�)
    private RTHandle tempColorTexture;
    private bool isRTHandleSupported;

    // Shader����ID����
    private static readonly int ShittimCanvasTexId = Shader.PropertyToID("_ShittimCanvasTex");
    private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
    private static readonly int PositionId = Shader.PropertyToID("_Position");
    private static readonly int ScaleId = Shader.PropertyToID("_Scale");
    private static readonly int BlendModeId = Shader.PropertyToID("_BlendMode");
    private static readonly int TilingId = Shader.PropertyToID("_Tiling");
    private static readonly int TilingCountId = Shader.PropertyToID("_TilingCount");

    // ��ʱRT����
    private const string TEMP_RT_NAME = "_TempShittimCanvasRT";

    public EnhancedShittimCanvasRenderPass(Material material, ShittimCanvasSettings settings)
    {
        this.material = material;
        this.settings = settings;
        this.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        // ���RTHandle֧��
#if UNITY_2022_1_OR_NEWER
        isRTHandleSupported = true;
#else
        isRTHandleSupported = false;
#endif
    }

    public void Setup(RenderTargetIdentifier colorTarget, ScriptableRenderer renderer)
    {
        this.colorTarget = colorTarget;
        this.renderer = renderer;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        ConfigureTarget(colorTarget);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        // ����ʱ��֤
        if (!ValidateExecution())
        {
            return;
        }

        CommandBuffer cmd = CommandBufferPool.Get("Enhanced ShittimCanvas");

        try
        {
            // ���²�������
            UpdateMaterialProperties();

            // ��ȡ��Ⱦ������
            RenderTextureDescriptor desc = GetRenderTextureDescriptor(renderingData);

            // ִ����Ⱦ
            ExecuteRendering(cmd, desc);
        }
        catch (System.Exception e)
        {
            //Debug.LogError($"[ShittimCanvas] ��Ⱦ�����з�������: {e.Message}");
        }
        finally
        {
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    private bool ValidateExecution()
    {
        if (material == null)
        {
            //Debug.LogError("[ShittimCanvas] MaterialΪ�գ��޷�ִ����Ⱦ");
            return false;
        }

        // ʹ��GetTexture������ȡ����
        Texture2D texture = settings?.GetTexture();
        if (texture == null)
        {
            //Debug.LogWarning("[ShittimCanvas] �޷���ȡˮӡ����������Ⱦ");
            return false;
        }

        return true;
    }

    private RenderTextureDescriptor GetRenderTextureDescriptor(RenderingData renderingData)
    {
        RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;
        desc.msaaSamples = 1; // ����MSAA�������������
        desc.useMipMap = false;
        desc.autoGenerateMips = false;

        return desc;
    }

    private void UpdateMaterialProperties()
    {
        if (material == null) return;

        Texture2D watermarkTexture = settings.GetTexture();
        material.SetTexture(ShittimCanvasTexId, watermarkTexture);
        material.SetFloat(OpacityId, settings.opacity);
        material.SetVector(PositionId, new Vector2(settings.positionX, settings.positionY));
        material.SetFloat(ScaleId, settings.scale);
        material.SetFloat(BlendModeId, (float)settings.blendMode);
        material.SetFloat(TilingId, settings.enableTiling ? 1f : 0f);
        material.SetFloat(TilingCountId, settings.tilingCount);
    }

    private void ExecuteRendering(CommandBuffer cmd, RenderTextureDescriptor desc)
    {
        // ������Ⱦʱ��ѡ����Ⱦ����
        switch (settings.renderEvent)
        {
            case ShittimCanvasRenderEvent.BeforePostProcessing:
            case ShittimCanvasRenderEvent.AfterPostProcessing:
            default:
                RenderWithBlit(cmd, desc);
                break;
        }
    }

    private void RenderWithBlit(CommandBuffer cmd, RenderTextureDescriptor desc)
    {
        // ��ȡ��ʱ��Ⱦ����
        int tempRTId = Shader.PropertyToID(TEMP_RT_NAME);
        cmd.GetTemporaryRT(tempRTId, desc, FilterMode.Bilinear);

        try
        {
            // ���Ƶ�ǰ��ɫ����������ʱ����
            cmd.Blit(colorTarget, tempRTId);

            // ʹ��ˮӡ������Ⱦ����ɫ������
            cmd.Blit(tempRTId, colorTarget, material, 0);
        }
        finally
        {
            // �ͷ���ʱ����
            cmd.ReleaseTemporaryRT(tempRTId);
        }
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        if (isRTHandleSupported && tempColorTexture != null)
        {
            tempColorTexture?.Release();
            tempColorTexture = null;
        }
    }

    public void Cleanup()
    {
        if (isRTHandleSupported && tempColorTexture != null)
        {
            tempColorTexture?.Release();
            tempColorTexture = null;
        }
    }
}