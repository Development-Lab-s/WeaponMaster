using BK._01_Script;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStartButton : MonoBehaviour,IButton
{
    [SerializeField]private int sceneIndex;
    public void Click()
    {
        GameStart();
    }
    public void GameStart()
    {
        SceneManager.LoadScene(sceneIndex);
    }
}
