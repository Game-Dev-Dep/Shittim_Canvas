using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class Spine_Services : MonoBehaviour
{
    public static Spine_Services Instance { get; set; }
    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[Awake] Spine Services 单例创建完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("UI Elements")]
    [SerializeField]
    public Button IK_Toggle_Button;
    [SerializeField]
    public RawImage IK_On_Image;
    [SerializeField]
    public RawImage IK_Off_Image;
    [SerializeField]
    public Button Talk_Toggle_Button;
    [SerializeField]
    public RawImage Talk_On_Image;
    [SerializeField]
    public RawImage Talk_Off_Image;

    [Header("Spine Settings")]
    public string Talk_M_Animation_Name = "Talk_{0}_M";
    public string Talk_A_Animation_Name = "Talk_{0}_A";
    public float Talk_Mix_To_Empty = 2f;


    [Header("Core Variable")]
    public bool is_IK_On = true;
    public bool is_Talk_On = true;

    private void Get_Config()
    {
        is_IK_On = Config_Services.Instance.Global_Function_Config.is_IK_On;
        Update_IK_Button();

        is_Talk_On = Config_Services.Instance.Global_Function_Config.is_Talk_On;
        Update_Talk_Button();
    }

    public void Set_Config()
    {
        Config_Services.Instance.Global_Function_Config.is_IK_On = is_IK_On;
        Config_Services.Instance.Global_Function_Config.is_Talk_On = is_Talk_On;
    }

    private void Start()
    {
        Get_Config();
        IK_Toggle_Button.onClick.AddListener(Toggle_IK);
        Talk_Toggle_Button.onClick.AddListener(Toggle_Talk);
    }

    public void Toggle_IK()
    {
        is_IK_On = !is_IK_On;
        Update_IK_Button();
    }

    public void Update_IK_Button()
    {
        IK_On_Image.enabled = is_IK_On;
        IK_Off_Image.enabled = !is_IK_On;
    }

    public void Toggle_Talk()
    {
        is_Talk_On =!is_Talk_On;
        Update_Talk_Button();
    }

    public void Update_Talk_Button()
    {
        Talk_On_Image.enabled = is_Talk_On;
        Talk_Off_Image.enabled = !is_Talk_On;
    }

    public IEnumerator Play_Talk_Clips(int index, SkeletonAnimation skeleton_animation, System.Action onComplete = null)
    {
        string full_talk_m_animation_name = string.Format(Talk_M_Animation_Name, index.ToString("D2"));
        string full_talk_a_animation_name = string.Format(Talk_A_Animation_Name, index.ToString("D2"));

        skeleton_animation.AnimationState.SetEmptyAnimation(1, Talk_Mix_To_Empty);
        skeleton_animation.AnimationState.AddAnimation(1, full_talk_m_animation_name, false, 0);

        skeleton_animation.AnimationState.SetEmptyAnimation(2, Talk_Mix_To_Empty);
        skeleton_animation.AnimationState.AddAnimation(2, full_talk_a_animation_name, false, 0);

        float duration = Mathf.Max(
            skeleton_animation.AnimationState.Data.SkeletonData.FindAnimation(full_talk_m_animation_name).Duration,
            skeleton_animation.AnimationState.Data.SkeletonData.FindAnimation(full_talk_a_animation_name).Duration
        );

        yield return new WaitForSeconds(duration);

        onComplete?.Invoke();
    }
}
