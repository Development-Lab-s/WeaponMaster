using System;
using System.ComponentModel.Design;
using _00.Scripts;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using WorkFolder.KJS.Server._00.Scripts.Lobby;

public class LobbyTabManager : MonoBehaviour
{
    [SerializeField] private RectTransform[] tabs;
    
    [Header("positions")]
    public RectTransform bottomPos;
    public RectTransform middlePos;
    public RectTransform upPos;
    
    [Header("tab")]
    public RectTransform tabBar;
    public RectTransform upPosition;
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
            
            if (i == _clickedTab && i == 1)
            {
                tabBar.DOAnchorPos(upPosition.anchoredPosition, 0.2f);
            }
            else if(i == _clickedTab)
            {
                continue;
            }
            tabs[i].gameObject.SetActive(false);
            
        }
        
        // for (int i = 0; i < tabs.Length; i++)
        // {
        //     if (i == _clickedTab)
        //     {
        //         tabs[i].DOAnchorPos(middlePos.anchoredPosition, 0.2f);
        //     }
        //     else
        //     {
        //         Debug.Log(tabs[i].gameObject);
        //         if (!tabs[i].gameObject.activeSelf)
        //         {
        //             continue;
        //         }
        //
        //         int i1 = i;
        //         tabs[i].DOAnchorPos(upPos.anchoredPosition, 0.2f).OnComplete(()=>
        //         {
        //             tabs[i1].anchoredPosition = bottomPos.anchoredPosition;
        //             tabs[i1].gameObject.SetActive(false);
        //         });
        //     }
        // }
    }

    
    
}
