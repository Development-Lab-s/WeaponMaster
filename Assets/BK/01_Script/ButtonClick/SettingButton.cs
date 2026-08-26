using System;
using BK._01_Script;
using UnityEngine;

public class SettingButton : MonoBehaviour, IButton
{
    [SerializeField] private GameObject gameSettingObject;
    
    public void Click()
    {
       OnSetting();
    }

    public void SettingOffClick()
    {
        OffSetting();
    }

    public void OnSetting()
    {
        gameSettingObject.SetActive(true);
    }
    public void OffSetting()
    {
        gameSettingObject.SetActive(false);
    }
}
