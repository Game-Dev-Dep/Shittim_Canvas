using UnityEngine;

/// <summary>
/// Control of the message that is created within the ToastNotification Hierarchy.
/// It is possible to access the controls of this class, but it is not recommended, as it is manipulated directly
/// by the class ToastNotification. 
/// </summary>
public class ToastNotificationMessage : MonoBehaviour
{
    [HideInInspector]
    public float messageTime; // Receives the time this message should be displayed on the screen
    [HideInInspector]
    public RectTransform timerRectTransform; // Receives the timer render component
    [HideInInspector]
    public bool leftToRight = true; // Which side will the timer decrease to which side?

    // Variables used in this class to make everything work well
    private float initialWidth;
    private float timeElapsed;


    // If the Start method is disturbing some part of your game's code, you can change it to Awake
    void Start()
    {
        messageTime = messageTime <= -1 ? ToastNotification.minimumMessageTime : messageTime;

        RectTransform messageRect = transform.parent.GetComponent<RectTransform>();
        timerRectTransform.sizeDelta = new Vector2(messageRect.sizeDelta.x, messageRect.sizeDelta.y * 0.07f );

        // Resets the position of the message on Canvas to be correctly positioned where it should be
        timerRectTransform.anchorMin = new Vector2(1,1);
        timerRectTransform.anchorMax = new Vector2(1,1);
        timerRectTransform.pivot = new Vector2(1,1);
        timerRectTransform.anchoredPosition = Vector3.zero;

        initialWidth = timerRectTransform.sizeDelta.x;
    }


    // You can change FixedUpdate to Update. But I recommend keeping it this way to consume less processing
    void FixedUpdate()
    {

        // FixedUpdate here basically works by checking whether the message has timed out,
        // and then calls the ToastNotification Hide command

        if (ToastNotification.isStoped == true)
            return;

        // Check for inifite messages
        if( messageTime != 0)
        {
            if (timeElapsed > messageTime)
            {
                // If there is a CanvasGroup, enable isHiding so that ToastNotification starts hiding it before closing
                if (ToastNotification.isCanvasGroup)
                    ToastNotification.isHiding = true;
                else
                    ToastNotification.Hide();
            }

            timeElapsed += Time.deltaTime;

            RenderTimer();
        }

    }

    // Renders the decreasing timer rectangle on the screen over the message
    void RenderTimer()
    {
        // Calculates the percentage of time remaining in relation to the total time
        float remainingTimePercentage = Mathf.Clamp01(1f - (timeElapsed / messageTime));

        // Calculates new timer width based on percentage of time remaining
        float newWidth = initialWidth * remainingTimePercentage;

        // Changes the calculation depending on which side the timer is going to reduce
        timerRectTransform.sizeDelta = new Vector2(newWidth, timerRectTransform.sizeDelta.y);
        if (leftToRight == false)
            timerRectTransform.anchoredPosition = new Vector2(-initialWidth + timerRectTransform.sizeDelta.x, timerRectTransform.anchoredPosition.y );

    }

    //Checks whether the hideOnClick variable is enabled in ToastNotification, so that it can only be closed when clicked
    // This function is here, instead of being in ToastNotification,
    //      just to facilitate the Click event that the message receives by default.
    // If for any reason you need it in ToastNotification, just copy from here and paste there
    public static void HideOnClick()
    {
        if (ToastNotification.hideOnClick)
            ToastNotification.Hide();
    }

}
