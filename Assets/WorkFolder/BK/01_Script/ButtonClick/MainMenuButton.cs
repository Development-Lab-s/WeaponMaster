using BK._01_Script;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButton : MonoBehaviour,IButton
{
    [SerializeField]private int mainMenuSceneIndex;
    public void Click()
    {
        GameStart();
    }
    public void GameStart()
    {
        SceneManager.LoadScene(mainMenuSceneIndex);
    }
}
