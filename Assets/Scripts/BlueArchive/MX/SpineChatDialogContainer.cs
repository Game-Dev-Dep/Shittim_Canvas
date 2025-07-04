using UnityEngine;
using UnityEngine.AddressableAssets;

public class SpineChatDialogContainer : MonoBehaviour // TypeDefIndex: 7526
{
    // Fields
    public AssetReference AssetReference; // 0x18
    public SpineCharacter SpineCharacter; // 0x20
    public Transform SpinePosition; // 0x28
    public UIWidget SpineRenderOrder; // 0x30
    public ChatDialog ChatDialog; // 0x38
    public DialogCategory DialogCategory; // 0x40
    public long CharacterId; // 0x48
    private DialogCondition condition; // 0x50
}
