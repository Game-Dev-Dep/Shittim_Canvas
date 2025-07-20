using UnityEngine;
using UnityEngine.SceneManagement;

/****************************************************
// Hey! If you use this code bellow, you can invoke ToastNotification just writing Show instead ToastNotification.Show ^_~

using static ToastNotification;

/**************************************************** */

public class ShowCase : MonoBehaviour
{

    private void Update()
    {
        // This is a example where you can use ToastNotification anywhere, like in a character script, item script, shop UI...
        // Anywhere you want, just call ToastNotification.Show :D
        if( Input.GetKeyDown( KeyCode.V ))
        {
            ToastNotification.Show("Yeah, a simple Key can display a message. And this message doens't have a \"timer\" display render", 10);
        }

        // You can setup a key/function/event/everthing to hide messages on screen. Very useful with infinite messages.
        // Example use case: "Press E to get into the car", after pressing the key, the message disappears
        if (Input.GetKeyDown(KeyCode.Z))
        {
            ToastNotification.Hide();
        }
    }


    /* Welcome to Toast Notification! */

public void ShowMessageDefault()
    {
        ToastNotification.Show("Hey there! How do you like this message? Pretty cool, huh?", 3, "success");
    }
    public void ShowMessageWhitoutIcon()
    {
        ToastNotification.Show("You don't have to use the icons if you don't want to");
    }
    public void ShowMessageWithoutTimer()
    {
        ToastNotification.Show("If the timer hits zero, this message will be infinite... click to close it or press Z key", 0, "info" );
    }

    /* Custom Messages Secene */

    public void ShowMessageNewSetup()
    {
        ToastNotification.Show("You can go for the light theme, but feel free to spice up your messages too!");
    }

    public void ShowCenterMsg()
    {
        ToastNotification.messageScreenPosition = MessageScreenPosition.Center;
        ToastNotification.Show("Messages in the center are cool, but be careful with the opacity, it can get in the way");
    }

    public void LeftBottomAndDark()
    {
        ToastNotification.messageScreenPosition = MessageScreenPosition.BottomRight;
        ToastNotification.darkTheme = true;
        ToastNotification.Show("If the messages start talking back to you, it might be time for a coffee break XD");
    }

    /* Scene manager */
    public void BackToHome()
    {
        SceneManager.LoadScene("ShowCase");
    }
    public void GoToCustom()
    {
        SceneManager.LoadScene("CustomMessages");
    }

}
