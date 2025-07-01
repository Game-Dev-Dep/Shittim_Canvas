using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CVCollectionType // TypeDefIndex: 8214
{
    CVNormal = 0,
    CVEvent = 1,
    CVEtc = 2
}

namespace MX.Data
{
    public abstract class CharacterDialogInfoBase // TypeDefIndex: 12412
    {
        // Properties
        public abstract long CharacterId { get; }
        public abstract long CostumeUniqueId { get; }
        public abstract long DisplayOrder { get; }
        public abstract DialogCategory DialogCategory { get; }
        public abstract DialogCondition DialogCondition { get; }
        public abstract long GroupId { get; }
        public abstract string ActionName { get; }
        public abstract long Duration { get; }
        public abstract DialogType DialogType { get; }
        public abstract string LocalizeKR { get; }
        public abstract string LocalizeJP { get; }
        //public abstract FlatArrayWrapper<uint> VoiceIds { get; }
        public abstract bool CollectionVisible { get; }
        public abstract CVCollectionType CVCollectionType { get; }
        public abstract string LocalizeCVGroup { get; }
        public abstract string AnimationName { get; }
    }
}