using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable]
public class ShittimCanvasSettings
{
    [Header("ShittimCanvas Texture")]
    [SerializeField] public Texture2D ShittimCanvasTexture;

    [Header("Fallback Options")]
    [SerializeField] private string textureResourcePath = ""; // Resources路径
    [SerializeField] private string textureGUID = ""; // 纹理的GUID，用于编辑器

    // 运行时纹理加载
    public Texture2D GetTexture()
    {
        // 首先尝试直接引用
        if (ShittimCanvasTexture != null)
        {
            return ShittimCanvasTexture;
        }

        // 尝试从Resources加载
        if (!string.IsNullOrEmpty(textureResourcePath))
        {
            Texture2D resourceTexture = Resources.Load<Texture2D>(textureResourcePath);
            if (resourceTexture != null)
            {
                //Debug.Log($"[ShittimCanvas] 从Resources加载纹理: {textureResourcePath}");
                ShittimCanvasTexture = resourceTexture;
                return resourceTexture;
            }
        }

#if UNITY_EDITOR
        // 编辑器中尝试通过GUID加载
        if (!string.IsNullOrEmpty(textureGUID))
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(textureGUID);
            if (!string.IsNullOrEmpty(assetPath))
            {
                Texture2D guidTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (guidTexture != null)
                {
                    //Debug.Log($"[ShittimCanvas] 通过GUID加载纹理: {assetPath}");
                    ShittimCanvasTexture = guidTexture;
                    return guidTexture;
                }
            }
        }
#endif

        // 创建默认纹理
        return CreateDefaultTexture();
    }

    private Texture2D CreateDefaultTexture()
    {
        //Debug.LogWarning("[ShittimCanvas] 创建默认水印纹理");

        Texture2D defaultTexture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[256 * 256];

        // 创建一个简单的默认水印图案
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
    // 编辑器中自动记录纹理信息
    public void RecordTextureInfo()
    {
        if (ShittimCanvasTexture != null)
        {
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(ShittimCanvasTexture);
            textureGUID = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);

            // 检查是否在Resources文件夹中
            if (assetPath.Contains("/Resources/"))
            {
                int resourcesIndex = assetPath.LastIndexOf("/Resources/") + "/Resources/".Length;
                string resourcePath = assetPath.Substring(resourcesIndex);
                // 移除文件扩展名
                int dotIndex = resourcePath.LastIndexOf('.');
                if (dotIndex > 0)
                {
                    resourcePath = resourcePath.Substring(0, dotIndex);
                }
                textureResourcePath = resourcePath;
                //Debug.Log($"[ShittimCanvas] 记录Resources路径: {textureResourcePath}");
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
    private Texture2D cachedTexture; // 缓存纹理引用

    // 缓存的Shader名称，避免运行时查找
    private const string SHADER_NAME = "UI/ShittimCanvas";

    public override void Create()
    {
        //Debug.Log("[ShittimCanvas] Create method called");

        // 确保在Create阶段创建材质和Pass
        InitializeMaterial();
        InitializeRenderPass();

        // 预加载纹理
        PreloadTexture();
    }

    private void PreloadTexture()
    {
        cachedTexture = settings.GetTexture();
        if (cachedTexture != null)
        {
            //Debug.Log($"[ShittimCanvas] 预加载纹理成功: {cachedTexture.name}");
        }
        else
        {
            //Debug.LogWarning("[ShittimCanvas] 预加载纹理失败");
        }
    }

    private void InitializeMaterial()
    {
        // 优先使用指定的Shader引用
        if (ShittimCanvasShader == null)
        {
            ShittimCanvasShader = Shader.Find(SHADER_NAME);
        }

        // 如果找不到Shader，使用备用Shader
        if (ShittimCanvasShader == null)
        {
            //Debug.LogWarning($"[ShittimCanvas] 无法找到Shader '{SHADER_NAME}'，使用默认Sprite Shader");
            ShittimCanvasShader = Shader.Find("Sprites/Default");
        }

        // 创建材质
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
        // 运行时验证和重建
        if (!ValidateAndRebuildIfNeeded())
        {
            return;
        }

        // 验证纹理（使用GetTexture方法）
        Texture2D currentTexture = settings.GetTexture();
        if (currentTexture == null)
        {
            //Debug.LogWarning("[ShittimCanvas] 无法获取水印纹理，跳过渲染");
            return;
        }

        // 更新缓存的纹理
        if (cachedTexture != currentTexture)
        {
            cachedTexture = currentTexture;
            //Debug.Log($"[ShittimCanvas] 更新纹理缓存: {cachedTexture.name}");
        }

        // 只在Game和SceneView相机中渲染
        if (renderingData.cameraData.cameraType != CameraType.Game &&
            renderingData.cameraData.cameraType != CameraType.SceneView)
        {
            return;
        }

        //Debug.Log("[ShittimCanvas] 添加渲染Pass");

        // 配置渲染Pass
        RenderPassEvent targetEvent = GetRenderPassEvent(settings.renderEvent);
        ShittimCanvasPass.renderPassEvent = targetEvent;
        ShittimCanvasPass.ConfigureInput(ScriptableRenderPassInput.Color);

        // 设置Pass并加入渲染队列
        ShittimCanvasPass.Setup(renderer.cameraColorTarget, renderingData.cameraData.renderer);
        renderer.EnqueuePass(ShittimCanvasPass);
    }

    private bool ValidateAndRebuildIfNeeded()
    {
        bool needsRebuild = false;

        // 检查Shader
        if (ShittimCanvasShader == null)
        {
            //Debug.LogWarning("[ShittimCanvas] Shader丢失，尝试重新查找");
            ShittimCanvasShader = Shader.Find(SHADER_NAME);
            needsRebuild = true;
        }

        // 检查Material
        if (ShittimCanvasMaterial == null && ShittimCanvasShader != null)
        {
            //Debug.LogWarning("[ShittimCanvas] Material丢失，重新创建");
            ShittimCanvasMaterial = CoreUtils.CreateEngineMaterial(ShittimCanvasShader);
            needsRebuild = true;
        }

        // 检查RenderPass
        if (ShittimCanvasPass == null && ShittimCanvasMaterial != null)
        {
            //Debug.LogWarning("[ShittimCanvas] RenderPass丢失，重新创建");
            ShittimCanvasPass = new EnhancedShittimCanvasRenderPass(ShittimCanvasMaterial, settings);
            needsRebuild = true;
        }

        // 最终验证
        if (ShittimCanvasShader == null)
        {
            //Debug.LogError("[ShittimCanvas] 无法创建Shader");
            return false;
        }

        if (ShittimCanvasMaterial == null)
        {
            //Debug.LogError("[ShittimCanvas] 无法创建Material");
            return false;
        }

        if (ShittimCanvasPass == null)
        {
            //Debug.LogError("[ShittimCanvas] 无法创建RenderPass");
            return false;
        }

        if (needsRebuild)
        {
            //Debug.Log("[ShittimCanvas] 组件重建完成");
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

    // 在编辑器中支持热重载
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

    // 使用RTHandle替代RenderTargetHandle (URP 12+推荐)
    private RTHandle tempColorTexture;
    private bool isRTHandleSupported;

    // Shader属性ID缓存
    private static readonly int ShittimCanvasTexId = Shader.PropertyToID("_ShittimCanvasTex");
    private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
    private static readonly int PositionId = Shader.PropertyToID("_Position");
    private static readonly int ScaleId = Shader.PropertyToID("_Scale");
    private static readonly int BlendModeId = Shader.PropertyToID("_BlendMode");
    private static readonly int TilingId = Shader.PropertyToID("_Tiling");
    private static readonly int TilingCountId = Shader.PropertyToID("_TilingCount");

    // 临时RT名称
    private const string TEMP_RT_NAME = "_TempShittimCanvasRT";

    public EnhancedShittimCanvasRenderPass(Material material, ShittimCanvasSettings settings)
    {
        this.material = material;
        this.settings = settings;
        this.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        // 检查RTHandle支持
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
        // 运行时验证
        if (!ValidateExecution())
        {
            return;
        }

        CommandBuffer cmd = CommandBufferPool.Get("Enhanced ShittimCanvas");

        try
        {
            // 更新材质属性
            UpdateMaterialProperties();

            // 获取渲染描述符
            RenderTextureDescriptor desc = GetRenderTextureDescriptor(renderingData);

            // 执行渲染
            ExecuteRendering(cmd, desc);
        }
        catch (System.Exception e)
        {
            //Debug.LogError($"[ShittimCanvas] 渲染过程中发生错误: {e.Message}");
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
            //Debug.LogError("[ShittimCanvas] Material为空，无法执行渲染");
            return false;
        }

        // 使用GetTexture方法获取纹理
        Texture2D texture = settings?.GetTexture();
        if (texture == null)
        {
            //Debug.LogWarning("[ShittimCanvas] 无法获取水印纹理，跳过渲染");
            return false;
        }

        return true;
    }

    private RenderTextureDescriptor GetRenderTextureDescriptor(RenderingData renderingData)
    {
        RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;
        desc.msaaSamples = 1; // 禁用MSAA避免兼容性问题
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
        // 根据渲染时机选择渲染策略
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
        // 获取临时渲染纹理
        int tempRTId = Shader.PropertyToID(TEMP_RT_NAME);
        cmd.GetTemporaryRT(tempRTId, desc, FilterMode.Bilinear);

        try
        {
            // 复制当前颜色缓冲区到临时纹理
            cmd.Blit(colorTarget, tempRTId);

            // 使用水印材质渲染到颜色缓冲区
            cmd.Blit(tempRTId, colorTarget, material, 0);
        }
        finally
        {
            // 释放临时纹理
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
