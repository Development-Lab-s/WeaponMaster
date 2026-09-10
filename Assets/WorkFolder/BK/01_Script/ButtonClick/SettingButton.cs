using System;
using System.Collections;
using BK._01_Script;
using UnityEngine;

public class SettingButton : MonoBehaviour, IButton
{
    [SerializeField] private GameObject gameSettingObject;
    [SerializeField]private SettingMove settingMove;
    /*[SerializeField] private Cloth cloth;*/
    
    public void Click()
    {
       //OnSetting();
       settingMove.SettingMoves();
       /*StartCoroutine(AccelationRoutine());*/
    }

    /*private IEnumerator AccelationRoutine()
    {
        cloth.externalAcceleration = new Vector3(4, 0, 4);
        yield return new WaitForSeconds(0.1f);
        cloth.externalAcceleration = new Vector3(-25, 0, -25);
        yield return new WaitForSeconds(0.1f);
        cloth.externalAcceleration = Vector3.zero;
    }*/

    public void SettingOffClick()
    {
       settingMove.SettingOffMove();
    }

    public void SettingSclseUpClick()
    {
        settingMove.ScaleUpSetting();
    }

    public void SettingSclseDownClick()
    {
        settingMove.ScaleDownSetting();
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
