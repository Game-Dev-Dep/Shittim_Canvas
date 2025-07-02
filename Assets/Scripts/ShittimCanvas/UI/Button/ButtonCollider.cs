using UnityEngine;
using UnityEngine.UI;

//按钮碰撞器，用于解决按钮点击区域不准确的问题，主要给斜着的BA按钮使用。
//实现方法也很简单，Alpha通道为0的区域不响应点击事件哈哈哈！

[RequireComponent(typeof(Image))]
public class ButtonCollider : MonoBehaviour
{
    public float threshold = 0.1f;  // 范围 0~1

    void Start()
    {
        var img = GetComponent<Image>();
        img.alphaHitTestMinimumThreshold = threshold;
    }
}
