using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Networking;
using System.Linq;

public class Shader_Services : MonoBehaviour
{
    public static Shader_Services Instance { get; set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Debug.Log("[Awake] Shader Services 单例创建完成");
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 替换所有AB包的材质为本地已有材质
    /// </summary>
    //public void Replace_All_Shader()
    //{
    //    Material[] materials = Resources.FindObjectsOfTypeAll<Material>();
    //    Console_Log($"开始替换 {materials.Length} 个材质");

    //    foreach (Material material in materials)
    //    {
    //        Console_Log($"AB包中存在 Material：{material.name}");

    //        Shader local_shader = Shader.Find(material.shader.name);
    //        if (local_shader != null && material.shader != local_shader)
    //        {
    //            Console_Log($"└本地存在 Shader：{material.shader.name}");
    //            int renderqueue = material.renderQueue;
    //            material.shader = local_shader;

    //            bool is_need_replace_texture = false;
    //            bool is_spine_texture = false;
    //            List<string> key_word_list = new List<string>();
    //            if (material.shader.name == "Spine/Skeleton MX Standard" ||
    //                material.shader.name == "Spine/Skeleton")
    //            {
    //                key_word_list.Add("_MainTex");
    //                is_spine_texture = true;
    //                is_need_replace_texture = true;
    //            }
    //            if (material.shader.name == "Sprites/Default" ||
    //                material.shader.name == "Mobile/Particles/Additive")
    //            {
    //                key_word_list.Add("_MainTex");
    //                is_need_replace_texture = true;
    //            }
    //            if (
    //                material.shader.name == "DSFX/FX_SHADER_Additive_0" ||
    //                material.shader.name == "DSFX/FX_SHADER_AlphaBlend_0" ||
    //                material.shader.name == "DSFX/FX_SHADER_AlphaBlend_2_Roz" ||
    //                material.shader.name == "DSFX/FX_SHADER_AlphaBlend_Add"
    //                )
    //            {
    //                key_word_list.Add("_Texture");
    //                is_need_replace_texture = true;
    //            }
    //            if (material.shader.name == "DSFX/FX_SHADER_AlphaBlend_Add_Distort_0")
    //            {
    //                key_word_list.Add("_Tex_Main");
    //                key_word_list.Add("_Tex_Mask");
    //                key_word_list.Add("_Tex_Distort");
    //                is_need_replace_texture = true;
    //            }
    //            if (material.shader.name == "FX/FX_SHADER_Lobby_Order")
    //            {
    //                key_word_list.Add("_TextureSample0");
    //                is_need_replace_texture = true;
    //            }
    //            if (
    //                material.shader.name == "DSFX/FX_SHADER_Matcap" || 
    //                material.shader.name == "DSFX/FX_SHADER_Matcap_0"
    //                )
    //            {
    //                key_word_list.Add("_Main_Tex");
    //                key_word_list.Add("_Matcap_Tex");
    //                is_need_replace_texture = true;
    //            }
    //            if (
    //                material.shader.name == "DSFX/FX_SHADER_AlphaBlend_Add_Mask_0" ||
    //                material.shader.name == "DSFX/FX_SHADER_Step_0"
    //                )
    //            {
    //                key_word_list.Add("_Tex_Main");
    //                key_word_list.Add("_Tex_Mask");
    //                is_need_replace_texture = true;
    //            }
    //            if (material.shader.name == "DSFX/FX_SHADER_Step_Distort_0")
    //            {
    //                key_word_list.Add("_Tex_Main");
    //                key_word_list.Add("_Tex_Mask");
    //                key_word_list.Add("_Tex_Distort");
    //                is_need_replace_texture = true;
    //            }
    //            if (is_need_replace_texture)
    //            {
    //                foreach (string key_word in key_word_list)
    //                {
    //                    if (
    //                        material.shader.name == "Sprites/Default" ||
    //                        material.shader.name == "Mobile/Particles/Additive"
    //                        )
    //                    {
    //                        material.SetTexture(key_word, Resources.Load<Texture2D>("Sprites/MySquare"));
    //                        break;
    //                    }

    //                    if (material.GetTexture(key_word) != null)
    //                    {
    //                        string ab_texture2d_name = material.GetTexture(key_word).name;
    //                        Console_Log($" └AB包中的 Material {material.name} 存在关键字段为 {key_word} 的 Texture: {ab_texture2d_name}");
    //                        //StartCoroutine(Get_Texture_By_Path(Path.Combine(is_spine_texture ? File_Services.Student_Textures_Folder_Path : File_Services.MX_Files_Textures_Folder_Path, ab_texture2d_name + ".png"), local_texture2d =>
    //                        //{
    //                        //    if (local_texture2d != null)
    //                        //    {
    //                        //        material.SetTexture(key_word, local_texture2d);
    //                        //    }
    //                        //}));
    //                    }
    //                    else
    //                    {
    //                        Console_Log($" └AB包中的 Material {material.name} 不存在关键字段为 {key_word} 的 Texture");
    //                    }
    //                }
    //            }

    //            material.renderQueue = renderqueue;
    //            if (material.renderQueue == 2000) material.renderQueue = 3000;
    //            continue;
    //        }
    //        else
    //        {
    //            Console_Log($"└本地不存在 Shader：{material.shader.name}");
    //        }
    //    }
    //    Console_Log($"结束替换 {materials.Length} 个材质");
    //}

    public void Replace_All_Spine_Shader()
    {
        Material[] materials = Resources.FindObjectsOfTypeAll<Material>();

        foreach (Material material in materials)
        {
            Shader local_shader = Shader.Find(material.shader.name);
            if (local_shader != null && material.shader != local_shader)
            {
                int renderqueue = material.renderQueue;
                material.shader = local_shader;

                if (
                    material.shader.name == "Spine/Skeleton MX Standard" ||
                    material.shader.name == "Spine/Skeleton" ||
                    material.shader.name == "Spine/Blend Modes/Skeleton PMA Multiply" ||
                    material.shader.name == "Spine/Skeleton MX Portrait"
                   )
                {
                    if (material.GetTexture("_MainTex") != null)
                    {
                        string ab_texture2d_name = material.GetTexture("_MainTex").name;
                        Console_Log($"AB包中的 Material {material.name} 存在关键字段为 _MainTex 的 Texture: {ab_texture2d_name}");
                        material.SetTexture("_MainTex", Texture_Services.Get_Texture_By_Path(Path.Combine(File_Services.Student_Textures_Folder_Path, ab_texture2d_name + ".png")));
                    }
                }

                material.renderQueue = renderqueue;
                if (material.renderQueue == 2000) material.renderQueue = 3000;
                continue;
            }
        }
    }

    public void Replace_Render_Shader(GameObject gameobject)
    {
        Renderer[] renderers = gameobject.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Console_Log($"GameObject: {renderer.gameObject.name} 下含有 Renderer 组件", Debug_Services.LogLevel.Core);
            if (renderer.sharedMaterials != null)
            {
                if (renderer.GetType() == typeof(SpriteRenderer))
                {
                    Console_Log($" └该 Renderer 组件为 SpriteRenderer，含有 {renderer.sharedMaterials.Length} 个 Material 组件", Debug_Services.LogLevel.Core);
                    Sprite ab_sprite = renderer.gameObject.GetComponent<SpriteRenderer>().sprite;
                    Material ab_material = renderer.gameObject.GetComponent<SpriteRenderer>().material;
                    Shader ab_shader = renderer.gameObject.GetComponent<SpriteRenderer>().material.shader;
                    Console_Log($"  └Sprite: {ab_sprite.name} Material: {ab_material.name} Shader: {ab_shader.name}", Debug_Services.LogLevel.Core);
                    if (ab_sprite != null)
                    {
                        Sprite local_sprite = Resources.Load<Sprite>($"Sprites/My_{ab_sprite.name}");
                        Shader local_shader = Shader.Find(ab_material.shader.name);
                        renderer.gameObject.GetComponent<SpriteRenderer>().sprite = local_sprite;
                        renderer.gameObject.GetComponent<SpriteRenderer>().material.shader = local_shader;
                    }

                    continue;
                }

                Console_Log($" └Renderer 组件下含有 {renderer.sharedMaterials.Length} 个 Material 组件", Debug_Services.LogLevel.Core);

                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    Material ab_material = renderer.sharedMaterials[i];
                    if (ab_material != null)
                    {
                        Shader local_shader = Shader.Find(ab_material.shader.name);
                        if (local_shader != null && local_shader != ab_material.shader)
                        {
                            int shader_property_count = ab_material.shader.GetPropertyCount();
                            Console_Log($"  └Material: {ab_material.name} 的 Shader: {ab_material.shader.name} 存在本地替换文件，有 {shader_property_count} 个字段", Debug_Services.LogLevel.Core);
                            Console_Log($"  └ABShader: {ab_material.shader.name} 替换为本地Shader: {local_shader.name}", Debug_Services.LogLevel.Core);

                            int ab_render_queue = ab_material.renderQueue;
                            ab_material.shader = local_shader;
                            ab_material.renderQueue = ab_render_queue;
                            if (ab_material.renderQueue == 2000) ab_material.renderQueue = 3000;
                            
                            for (int j = 0; j < shader_property_count; j++)
                            {
                                string shader_property_name = ab_material.shader.GetPropertyName(j);
                                ShaderPropertyType propertyType = ab_material.shader.GetPropertyType(j);
                                if (propertyType == ShaderPropertyType.Texture && ab_material.GetTexture(shader_property_name) != null)
                                {
                                    string ab_texture2d_name = ab_material.GetTexture(shader_property_name).name;
                                    TextureWrapMode ab_texture_wrapmode = ab_material.GetTexture(shader_property_name).wrapMode;

                                    Console_Log($"   └字段 {shader_property_name} 为 {ab_texture_wrapmode} 模式的 Texture: {ab_texture2d_name} ", Debug_Services.LogLevel.Core);
                                    bool is_spine_texture = (
                                                             ab_material.shader.name == "Spine/Skeleton MX Standard" ||
                                                             ab_material.shader.name == "Spine/Skeleton" ||
                                                             ab_material.shader.name == "Spine/Blend Modes/Skeleton PMA Multiply" ||
                                                             ab_material.shader.name == "Spine/Skeleton MX Portrait"
                                                             ) ? true : false;
                                    ab_material.SetTexture(shader_property_name, Texture_Services.Get_Texture_By_Path(Path.Combine(is_spine_texture ? File_Services.Student_Textures_Folder_Path : File_Services.MX_Files_Textures_Folder_Path, ab_texture2d_name + ".png")));
                                    ab_material.GetTexture(shader_property_name).wrapMode = ab_texture_wrapmode;
                                }
                            }

                        }
                        else
                        {
                            Console_Log($"  └Material {ab_material.name} 的 Shader {ab_material.shader.name} 不存在本地替换文件", Debug_Services.LogLevel.Core);
                        }
                    }
                }
            }
        }
    }

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Shader_Services", message, loglevel, logtype); }
}