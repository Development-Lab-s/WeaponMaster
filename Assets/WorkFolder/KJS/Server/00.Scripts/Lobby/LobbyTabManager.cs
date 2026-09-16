using System;
using System.ComponentModel.Design;
using Unity.VisualScripting;
using UnityEngine;

public class LobbyTabManager : MonoBehaviour
{
    [SerializeField] private GameObject[] tabs;
    private static LobbyTabManager _instance;
    public static LobbyTabManager Instance => _instance;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance =  this;
        }
    }
    
    public void CloseTheTabs(int clickedTab)
    {
        int _clickedTab =  clickedTab;
        
        for (int i = 0; i < tabs.Length; i++)
        {
            if (i == _clickedTab) continue;
            tabs[i].SetActive(false);
        }
    }

    
    
}
