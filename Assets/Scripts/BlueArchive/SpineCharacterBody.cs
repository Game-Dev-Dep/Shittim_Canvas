using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpineCharacterBodyTouch : MonoBehaviour
{
    public SpineCharacter SpineCharacter;
    public int Cur_Talk_Index = 1;
    public bool is_Talking = false;

    private void Update()
    {
        if (Wallpaper_Services.Instance.is_Wallpaper_Mode)
        {
            if (Index_Services.Instance.Cur_Responsing_Object != gameObject) return;

            if (Wallpaper_Mode_Handler.Instance.is_Released)
            {
                Wallpaper_Mode_Handler.Instance.is_Released = false;
                Index_Services.Instance.Cur_Responsing_Object = null;
            }

            if (Wallpaper_Mode_Handler.Instance.is_Pressed)
            {
                OnClick();
                Wallpaper_Mode_Handler.Instance.is_Pressed = false;
            }
        }
    }

    private void OnClick()
    {
        //Debug.Log($"{gameObject.name} OnClick 触发");

        if (!Spine_Services.Instance.is_Talk_On) return;
        if (!Index_Services.Instance.is_Idle_Mode) return;
        if (Index_Services.Instance.Talk_Animaiton_Num == 0) return;
        if (Wallpaper_Services.Instance.is_Wallpaper_Mode)
        {
            if (Window_Services.Instance.Cur_Cover_Window_Type != Window_Services.Cover_Window_Type.No_Window) return;
            if (gameObject != Index_Services.Instance.Cur_Responsing_Object) return;
        }

        if (!is_Talking)
        {
            is_Talking = true;
            Index_Services.Instance.is_Talking = true;
            Console_Log($"Talk_0{Cur_Talk_Index} 开始");
            StartCoroutine(Spine_Services.Instance.Play_Talk_Clips(Cur_Talk_Index, SpineCharacter.SkeletonAnimation, () =>
            {
                Console_Log($"Talk_0{Cur_Talk_Index} 结束");
                is_Talking = false;
                Index_Services.Instance.is_Talking = false;
                if (Cur_Talk_Index + 1 > Index_Services.Instance.Talk_Animaiton_Num) Cur_Talk_Index = 1;
                else Cur_Talk_Index++;
            }));
        }
    }

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("SpineCharacterBody", message, loglevel, logtype); }
}
