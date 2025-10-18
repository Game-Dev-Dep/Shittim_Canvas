using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Fade_Services : MonoBehaviour
{
    public static Fade_Services Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[Awake] Fade Services 单例创建完成");
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private Image fadeImage;
    private Canvas fadeCanvas;

    private void Start()
    {
        CreateFadeCanvas();
    }

    private void CreateFadeCanvas()
    {
        GameObject canvasObj = new GameObject("Fade_Canvas");
        canvasObj.transform.SetParent(transform);
        
        fadeCanvas = canvasObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 9999;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        GameObject imageObj = new GameObject("Fade_Image");
        imageObj.transform.SetParent(canvasObj.transform);
        
        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0);
        
        RectTransform rt = imageObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        
        canvasObj.SetActive(false);
    }

    public IEnumerator FadeOut(float duration)
    {
        fadeCanvas.gameObject.SetActive(true);
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0, 1, elapsed / duration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        
        fadeImage.color = Color.black;
    }

    public IEnumerator FadeIn(float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1, 0, elapsed / duration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        
        fadeImage.color = new Color(0, 0, 0, 0);
        fadeCanvas.gameObject.SetActive(false);
    }
}

