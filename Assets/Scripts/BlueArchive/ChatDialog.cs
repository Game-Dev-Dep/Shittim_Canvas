using MX.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public enum DialogCategory // TypeDefIndex: 8352
{
    Cafe = 0,
    Echelon = 1,
    CharacterSSRNew = 2,
    CharacterGet = 3,
    Birthday = 4,
    Dating = 5,
    Title = 6,
    UILobby = 7,
    UILobbySpecial = 8,
    UIShop = 9,
    UIGacha = 10,
    UIRaidLobby = 11,
    UIWork = 12,
    UITitle = 13,
    UIWeekDungeon = 14,
    UIAcademyLobby = 15,
    UIRaidLobbySeasonOff = 16,
    UIRaidLobbySeasonOn = 17,
    UIWorkAronaSit = 18,
    UIWorkAronaSleep = 19,
    UIWorkAronaWatch = 20,
    UIGuideMission = 21,
    UILobby2 = 22,
    UIClanSearchList = 23,
    UIAttendance = 24,
    UIAttendanceEvent01 = 25,
    UIEventLobby = 26,
    UIEventShop = 27,
    UIEventBoxGachaShop = 28,
    UIAttendanceEvent02 = 29,
    UIAttendanceEvent03 = 30,
    UIEventCardShop = 31,
    UISchoolDungeon = 32,
    UIAttendanceEvent = 33,
    UISpecialOperationLobby = 34,
    WeaponGet = 35,
    UIAttendanceEvent04 = 36,
    UIEventFortuneGachaShop = 37,
    UIAttendanceEvent05 = 38,
    UIAttendanceEvent06 = 39,
    UIMission = 40,
    UIEventMission = 41,
    UIAttendanceEvent08 = 42,
    UIAttendanceEvent07 = 43,
    UIEventMiniGameMission = 44,
    UIAttendanceEvent09 = 45,
    UIAttendanceEvent10 = 46,
    UIAttendanceEvent11 = 47,
    UIWorkPlanaSit = 48,
    UIWorkPlanaUmbrella = 49,
    UIWorkPlanaCabinet = 50,
    UIWorkCoexist_AronaSleepSit = 51,
    UIWorkCoexist_PlanaWatchSky = 52,
    UIWorkCoexist_PlanaSitPeek = 53,
    UIWorkCoexist_AronaSleepPeek = 54,
    UIEventArchive = 55,
    UIAttendanceEvent12 = 56,
    UIAttendanceEvent13 = 57,
    UIAttendanceEvent14 = 58,
    Temp_1 = 59,
    Temp_2 = 60,
    Temp_3 = 61,
    Temp_4 = 62,
    Temp_5 = 63,
    UIAttendanceEvent15 = 64,
    UILobbySpecial2 = 65,
    UIAttendanceEvent16 = 66,
    UIEventTreasure = 67,
    UIMultiFloorRaid = 68,
    UIEventMiniGameDreamMaker = 69,
    UIAttendanceEvent17 = 70,
    UIAttendanceEvent18 = 71
}
public enum DialogCondition // TypeDefIndex: 8353
{
     Idle = 0,
     Enter = 1,
     Exit = 2,
     Buy = 3,
     SoldOut = 4,
     BoxGachaNormal = 5,
     BoxGachaPrize = 6,
     Prize0 = 7,
     Prize1 = 8,
     Prize2 = 9,
     Prize3 = 10,
     Interaction = 11,
     Luck0 = 12,
     Luck1 = 13,
     Luck2 = 14,
     Luck3 = 15,
     Luck4 = 16,
     Luck5 = 17,
     StoryOpen = 18,
     CollectionOpen = 19,
     BoxGachaFinish = 20,
     FindTreasure = 21,
     GetTreasure = 22,
     RoundRenewal = 23,
     MiniGameDreamMakerEnough01 = 24,
     MiniGameDreamMakerEnough02 = 25,
     MiniGameDreamMakerEnough03 = 26,
     MiniGameDreamMakerEnough04 = 27,
     MiniGameDreamMakerDefault = 28
}
public enum DialogType
{
    Talk = 0,
    Think = 1,
    UITalk = 2
}

[AddComponentMenu("MX/UILobby/ChatDialog")]
public class ChatDialog : MonoBehaviour
{
    [Serializable]
    public class DialogBox // TypeDefIndex: 5616
    {
	    // Fields
	    public DialogType Type; // 0x10
        public GameObject GameObject; // 0x18
        public UILabel Text; // 0x20
        public UISprite Bg; // 0x28
        public TweenAlpha TextTween; // 0x30
        public TweenAlpha ImageTween; // 0x38
        private Transform myTransform; // 0x40
    }

    public List<DialogBox> DialogBoxes; // 0x18
    public Coroutine coroutine; // 0x20
    [SerializeField]
    public Animator spineAnimator; // 0x28
    public SpineCharacter spineCharacter; // 0x30
    public PortraitSpineCharacter portraitSpineCharacter; // 0x38
    public bool DialogEnable; // 0x68

    public bool IsPlaying { get; }
    public long CharacterId { get; set; }
    public long CostumeId { get; set; }
    public long prevGroupId { get; set; }
    public DialogCategory DialogCategory { get; set; }
    public DialogCondition Condition { get; set; }
    public List<List<CharacterDialogInfo>> AllIdleDialogs { get; set; }
}
