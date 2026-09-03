using BK._01_Script;
using UnityEngine;

public class QuitGameButton : MonoBehaviour,IButton
{
    public void Click()
    {
        QuitGame();
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
