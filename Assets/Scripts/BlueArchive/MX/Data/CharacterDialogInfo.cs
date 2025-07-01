using MX.Excel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MX.Data
{
    public class CharacterDialogInfo : CharacterDialogInfoBase // TypeDefIndex: 12411
    {
        // Fields
        private CharacterDialogExcel Excel; // 0x10

        // Properties
        public override long CharacterId { get; }
        public override long CostumeUniqueId { get; }
        public override long DisplayOrder { get; }
        public override DialogCategory DialogCategory { get; }
        public override DialogCondition DialogCondition { get; }
        public override long GroupId { get; }
        public override string ActionName { get; }
        public override DialogType DialogType { get; }
        public override long Duration { get; }
        public override string LocalizeKR { get; }
        public override string LocalizeJP { get; }
        //public override FlatArrayWrapper<uint> VoiceIds { get; }
        public override bool CollectionVisible { get; }
        public override CVCollectionType CVCollectionType { get; }
        public override string LocalizeCVGroup { get; }
        public override string AnimationName { get; }
        public Anniversary Anniversary { get; }
        public string StartDate { get; }
        public string EndDate { get; }
        public bool ApplyPosition { get; }
        public float PosX { get; }
        public float PosY { get; }
        public long UnlockFavorRank { get; }
        public bool UnlockEquipWeapon { get; }
    }
    }