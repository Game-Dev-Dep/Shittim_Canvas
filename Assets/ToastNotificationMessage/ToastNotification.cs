using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// State the position of the message on the screen.
public enum MessageScreenPosition { TopLeft, TopCenter, TopRight, Center, BottomLeft, BottomCenter, BottomRight }
// State on the side where the timer is decreased on the screen
public enum TimerDirection { LeftToRight, RightToLeft }

/// <summary>
/// Hierarchy ToastNotification object control. Use this class to call the Show (and its overloads) and Hide methods.
/// You can handle public static variables to customize the way messages appear on the screen,
/// but by doing so, you will override the configuration of all subsequent messages.
/// </summary>
public class ToastNotification : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    // The prefab used to display messages. Please choose one prefab on root folder. You can also create your own message prefab
    public Transform _messagePrefab;

    // Public static variables accessible throughout the project
    // Be careful when changing them at runtime, as as static variables, this will override existing settings
    public static bool isStoped = false; // Set this variable to stop the timer of messages on screen 
    public static bool showTimerRender; // Hide/show message timer rendering
    public static TimerDirection timerDirection; // Side where the timer is decreased on the screen
    public static MessageScreenPosition messageScreenPosition; // Position of the message on the screen.
    public static Vector2 margin; // Margin X an Y of messages on screen
    public static bool darkTheme; // Set DarkTheme (true) or LightTheme (false)
    public static float minimumMessageTime = 3; // Minimum time that all messages will remain on the screen
    public static bool hideOnClick = true; // Allow/disable hide messages on click
    public static bool isHiding = false; // Check if can animate with Fade effect a message
    public static bool isCanvasGroup = false; // Check if has a CanvasGroup in toastNotification object

    // Private static variables
    private static Transform messagePrefab; // Get the public prefab in a static variable
    private static Transform toastNotification; // Get the object that this script is linked to this static variable

    // Default message patterns configurable in the Unity Editor
    [Header("Default Message Patterns:")]
    [Tooltip("A countdown image will be displayed on message as a timer")]
    public bool _showTimerRender = true;
    [Tooltip("Disable it to use the default Light Theme on messages")]
    public bool _darkTheme = true;
    [Tooltip("Minimun time that all messages will be displayed.")]
    public float _minimumMessageTime = 3;
    [Tooltip("Margin X and Y on the corners. Margin X doens't works with centralized messages.")]
    public Vector2 _margin = new Vector2(20, 20);
    [Tooltip("Stop the timer when mouse cursor is over the ToastNotification object")]
    public bool _stopOnOver = true;
    [Tooltip("Hide/dismiss the message when it's clicked")]
    public bool _hideOnClick = true;
    [Tooltip("Position of messages on screen")]
    public MessageScreenPosition _messageScreenPosition = MessageScreenPosition.TopRight;
    [Tooltip("Direction of timer countdown. Auto will choose the best position relative to the Message Screen Position option.")]
    public TimerDirection _timerDirection = TimerDirection.LeftToRight;

    // Awake function called when the script instance is being loaded
    // You can change Awake to Start if this is causing problems with other scripts in your game
    void Awake()
    {

        // Assign public variables to their static counterparts
        messagePrefab = _messagePrefab;
        toastNotification = transform;

        minimumMessageTime = _minimumMessageTime;
        hideOnClick = _hideOnClick;
        darkTheme = _darkTheme;
        showTimerRender = _showTimerRender;
        timerDirection = _timerDirection;
        messageScreenPosition = _messageScreenPosition;
        margin = _margin;

        // Setup toastNotification object to works correctly in any environment 
        setupToastNotificationObject();
        void setupToastNotificationObject(){
            // Check if the ToastNotification object has a CanvasGroup component
            // If you don't add it, there will be no fade animations in your messages
            if (toastNotification.GetComponent<CanvasGroup>())
                isCanvasGroup = true;

            // Set the RectTransform properties for positioning
            // The messages' parent object must be completely reset to function correctly
            toastNotification.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0);
            toastNotification.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0);
            toastNotification.GetComponent<RectTransform>().pivot = new Vector2(0, 0);
        }

    }

    // You can change FixedUpdate to Update. But I recommend keeping it this way to consume less processing
    private void FixedUpdate()
    {
        // The FixedUpdate is used only to show/hide messages in an animated way with fade.
        // If there was no CanvasGroup, there is no fade, and this loop is not necessary
        if (!isCanvasGroup)
            return;

        // Hide effect process
        if ( isHiding)
        {
            toastNotification.GetComponent<CanvasGroup>().alpha -= 0.08f;
            if (toastNotification.GetComponent<CanvasGroup>().alpha < 0.01f)
            {
                // When is completely hided, call Hide function to dismiss the message
                Hide();
                isHiding = false;
            }
        }
        // Show message process
        else if (toastNotification.GetComponent<CanvasGroup>().alpha < 1)
        {
            
            toastNotification.GetComponent<CanvasGroup>().alpha += 0.05f;
        }

    }

    // Interface function triggered when mouse pointer enters the object
    public void OnPointerEnter(PointerEventData eventData)
    {
        // If stopOnOver is enabled, stop the timer
        if ( _stopOnOver )
            isStoped = true;
    }
    // Interface function triggered when mouse pointer exits the object
    public void OnPointerExit(PointerEventData eventData)
    {
        // If stopOnOver is enabled, resume the timer
        if ( _stopOnOver )
            isStoped = false;
    }

    // ----------- OVERLOADS ----------- 
    // Message text is the minimun necessary
    public static void Show(string messageText)
    {
        Show(messageText, minimumMessageTime, "");
    }
    // Text and timer
    public static void Show(string messageText, float timerInSeconds)
    {
        Show(messageText, timerInSeconds, "");
    }
    // Text and icon
    public static void Show(string messageText, string iconName)
    {
        Show(messageText, minimumMessageTime, iconName);
    }
    // ---------------------------------- 

    public static void Show( string messageText, float timerInSeconds = -1, string iconName = "")
    {

        // Hide any existing messages
        Hide();

        // If timerInSeconds is not provided, set it to the default minimumMessageTime
        // This can be zero, so the message will be infinite on the screen.
        if ( timerInSeconds <= -1 )
            timerInSeconds = minimumMessageTime;

        // Instantiate message prefab and configure it
        Transform message = Instantiate(messagePrefab, toastNotification);
        message.gameObject.SetActive(true);
        message.name = "Message"; // <- You can change the name of messages that are created here
        if ( isCanvasGroup ) toastNotification.GetComponent<CanvasGroup>().alpha = 0; // Instatiate with zero alpha (invisible)

        // Get IMAGE component of text, icons, timer and background
        TextMeshProUGUI text = message.Find("Text").GetComponent<TextMeshProUGUI>();
        UnityEngine.UI.Image background = message.Find("Background").GetComponent<UnityEngine.UI.Image>();
        Transform icons = message.Find("Icons");
        UnityEngine.UI.Image timer = message.Find("Timer").GetComponent<UnityEngine.UI.Image>();
        UnityEngine.UI.Image selectedIcon = null;

        // Useful variables that will be used by some functions below.
        Vector2 backgroundSize = background.GetComponent<RectTransform>().sizeDelta;
        RectTransform parentRect = toastNotification.GetComponent<RectTransform>();

        // Start of the chain of functions that configures a new message on the screen
        SetText();
        SetMessageIcon();
        SetMessageColor();
        SetupInvokeMessage();
        ResetToastNoticationPosition();
        SetMessagePositionOnScreen();

        // Set the message text and his aligment
        // You can change the text alignment at any time using something like:
        // GameObject.Find("ToastNotification").transform.Find("Text").GetComponent<TextMeshProUGUI>().aligment = ...
        void SetText()
        {
            text.text = messageText;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            if( messageScreenPosition == MessageScreenPosition.Center )
                text.alignment = TextAlignmentOptions.Center;
        }

        // Mount the icon chosen in the message on the screen
        // or adapt the message so as not to display it if  if the parameter was not passed
        void SetMessageIcon()
        {
            // If a iconName was passed
            if( iconName != "") 
            {
                iconName = Capitalize(iconName); // You can use your Capitalize function for free. Just call ToastNotification.Capitalize
                selectedIcon = icons.Find(iconName).transform.GetComponent<UnityEngine.UI.Image>(); // Find icon in child
                selectedIcon.enabled = true;
            }
            // If an icon is not passed, the entire message needs to be adapted to remove the extra space from the background
            else
            {
                float iconSize = icons.GetChild(0).GetComponent<RectTransform>().sizeDelta.x;
                backgroundSize = new Vector2( backgroundSize.x - iconSize - iconSize / 2 , backgroundSize.y);
                background.GetComponent<RectTransform>().sizeDelta = backgroundSize;
                Vector2 newAnchor = background.GetComponent<RectTransform>().anchoredPosition;
                newAnchor = new Vector2(newAnchor.x - iconSize, newAnchor.y);
                toastNotification.GetComponent<RectTransform>().anchoredPosition = newAnchor;
                text.GetComponent<RectTransform>().sizeDelta *= 0.90f ;
                text.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            }
        }

        // Changes the "theme" of the message if it is not chosen as DarkTheme
        void SetMessageColor()
        {
            // Below is the default dark theme color setting if you happen to need it:
            // Dark theme default
            /*
            Color foreColor = text.color = new Color(255, 255, 255, 1);
            Color backgroundColor = new Color(0.26f, 0.26f, 0.26f, 0.78f);
            */
            if (darkTheme == true)
                return;

            // White theme de fault
            Color foreColor = new Color(0.26f, 0.26f, 0.26f, 1);
            Color backgroundColor = new Color(255, 255, 255, 0.78f);
            // SecondaryColor (timer element) is based on foreColor
            Color secondaryColor = foreColor;
            secondaryColor.a = 0.39f;

            text.color = foreColor;
            background.color = backgroundColor;
            timer.color = secondaryColor;
            if (selectedIcon != null)
                selectedIcon.color = new Color( foreColor.r, foreColor.g, foreColor.b, 0.7f );
            
        }

        // Set the settings created in this parent object to the instantiated child object, which is the Message
        void SetupInvokeMessage()
        {
            ToastNotificationMessage toastNotificationMessage = message.GetComponent<ToastNotificationMessage>();
            toastNotificationMessage.timerRectTransform = timer.GetComponent<RectTransform>();
            toastNotificationMessage.messageTime = timerInSeconds;

            timer.enabled = showTimerRender;
            timer.enabled = timerInSeconds == 0 ? false : timer.enabled;
            toastNotificationMessage.leftToRight = timerDirection == TimerDirection.LeftToRight;

        }

        // Guides the position of the objects, as it may change in size, for example, if an icon was not passed
        void ResetToastNoticationPosition()
        {
            parentRect.anchoredPosition = Vector3.zero;

            message.GetComponent<RectTransform>().sizeDelta = backgroundSize;
            parentRect.sizeDelta = backgroundSize;
        }

        // Changes the position of the message on the screen, using the RectTransform anchor and its position.
            // Note that all guidance is based on the size of the Background,
            // so if you want to change the default prefab to create your own,
            // remember that everything must be contained within the background size!
        void SetMessagePositionOnScreen()
        {

            RectTransform parentRect = toastNotification.GetComponent<RectTransform>();
            Vector2 backgroundSize = background.GetComponent<RectTransform>().sizeDelta;

            if (messageScreenPosition == MessageScreenPosition.TopLeft)
            {
                parentRect.anchorMax = new Vector2(0, 1);
                parentRect.anchorMin = new Vector2(0, 1);
                parentRect.anchoredPosition = new Vector2(margin.x, -backgroundSize.y - margin.y);
            }
            else if (messageScreenPosition == MessageScreenPosition.TopRight)
            {
                parentRect.anchorMax = new Vector2(1, 1);
                parentRect.anchorMin = new Vector2(1, 1);
                parentRect.anchoredPosition = new Vector2(-backgroundSize.x - margin.x, -backgroundSize.y - margin.y);
            }
            else if (messageScreenPosition == MessageScreenPosition.TopCenter)
            {
                parentRect.anchorMax = new Vector2(0.5f, 1);
                parentRect.anchorMin = new Vector2(0.5f, 1);
                parentRect.anchoredPosition = new Vector2(-backgroundSize.x / 2, -backgroundSize.y - margin.y);
            }
            else if (messageScreenPosition == MessageScreenPosition.BottomLeft)
            {
                parentRect.anchorMax = new Vector2(0, 0);
                parentRect.anchorMin = new Vector2(0, 0);
                parentRect.anchoredPosition = new Vector2(margin.x, margin.y);
            }
            else if (messageScreenPosition == MessageScreenPosition.BottomCenter)
            {
                parentRect.anchorMax = new Vector2(0.5f, 0);
                parentRect.anchorMin = new Vector2(0.5f, 0);
                parentRect.anchoredPosition = new Vector2(-backgroundSize.x / 2, margin.y);
            }
            else if (messageScreenPosition == MessageScreenPosition.BottomRight)
            {
                parentRect.anchorMax = new Vector2(1, 0);
                parentRect.anchorMin = new Vector2(1, 0);
                parentRect.anchoredPosition = new Vector2(-backgroundSize.x - margin.x, margin.y);
            }
            else // Center
            {
                parentRect.anchorMax = new Vector2(0.5f, 0.5f);
                parentRect.anchorMin = new Vector2(0.5f, 0.5f);
                parentRect.anchoredPosition = new Vector2(-backgroundSize.x / 2, -backgroundSize.y / 2);
            }
        }

    }

    // "HIDE" is nothing more than destroying the object on the screen. This function can be called at any time
    public static void Hide()
    {
        if (toastNotification.childCount <= 0)
            return;
        for (int i = 0; i < toastNotification.childCount; i++)
        {
            if (toastNotification.GetChild(i).gameObject.activeSelf == true)
                Destroy(toastNotification.GetChild(i).gameObject);
        }
    }

    #region Utilities Functions

    // Do you want to capitalize the first letter? Use my capitalize function :)
    // Just make it public
    private static string Capitalize( string text )
    {
        if (string.IsNullOrEmpty(text))
            return text;
        else
            return char.ToUpper(text[0]) + text.Substring(1).ToLower();
    }

    #endregion

}
