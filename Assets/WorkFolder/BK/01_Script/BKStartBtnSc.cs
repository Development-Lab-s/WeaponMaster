using UnityEngine;
using UnityEngine.SceneManagement;

public class BKStartBtnSc : MonoBehaviour
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
