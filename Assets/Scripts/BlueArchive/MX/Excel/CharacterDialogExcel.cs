using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ProductionStep // TypeDefIndex: 8211
{
    ToDo = 0,
    Doing = 1,
    Complete = 2,
    Release = 3
}
public enum Anniversary // TypeDefIndex: 8356
{
    None = 0,
    UserBDay = 1,
    StudentBDay = 2
}

namespace MX.Excel
{
    public struct CharacterDialogExcel
    {

        // Properties
        public long CharacterId { get; }
        public long CostumeUniqueId { get; }
        public long DisplayOrder { get; }
        public ProductionStep ProductionStep { get; }
        public DialogCategory DialogCategory { get; }
        public DialogCondition DialogCondition { get; }
        public Anniversary Anniversary { get; }
        public string StartDate { get; }
        public string EndDate { get; }
        public long GroupId { get; }
        public DialogType DialogType { get; }
        public string ActionName { get; }
        public long Duration { get; }
        public string AnimationName { get; }
        public string LocalizeKR { get; }
        public string LocalizeJP { get; }
        public int VoiceIdLength { get; }
        public bool ApplyPosition { get; }
        public float PosX { get; }
        public float PosY { get; }
        public bool CollectionVisible { get; }
        public CVCollectionType CVCollectionType { get; }
        public long UnlockFavorRank { get; }
        public bool UnlockEquipWeapon { get; }
        public string LocalizeCVGroup { get; }
    }
}
