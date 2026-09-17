using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem;

public class SettingMove : MonoBehaviour
{
 [Header("Setting")]
[SerializeField]private GameObject setting;
 
 [Header("Panel")]
 [SerializeField]private GameObject settingPanel;
 [SerializeField] private GameObject title;
 [SerializeField] private GameObject closeBTM;
 
 [Header("Sttings")]
 [SerializeField]private GameObject allSettings;
 
 [Header("SOUND")]
 [SerializeField]private GameObject soundSetting;
 [SerializeField]private GameObject masterSetting;
 [SerializeField]private GameObject vfxSetting;
 [SerializeField]private GameObject bgmSetting;
 
 [Header("MOUSE")]
 [SerializeField]private GameObject mouseSetting;
 
 [Header("DEBUG")]
 [SerializeField]private GameObject debugSetting;
 

 private void Start()
 {
     setting.transform.DOLocalMoveX(2500, 0.0001f);
     soundSetting.transform.DOLocalMoveX(500, 0.0001f);
     masterSetting.transform.DOLocalMoveX(800, 0.0001f);
     vfxSetting.transform.DOLocalMoveX(1000, 0.0001f);
     bgmSetting.transform.DOLocalMoveX(1200, 0.0001f);
     
     mouseSetting.transform.DOLocalMoveX(500, 0.0001f);
     
     debugSetting.transform.DOLocalMoveX(500, 0.0001f);
 }

 public void SettingMoves()
 {
     setting.transform.DOLocalMoveX(26, 1f);
     
     soundSetting.transform.DOLocalMoveX( 550, 1.2f); //원래 300
     masterSetting.transform.DOLocalMoveX(0, 1.4f); //300
     vfxSetting.transform.DOLocalMoveX(0, 1.45f); // 300
     bgmSetting.transform.DOLocalMoveX(0, 1.6f); // 300
     
     mouseSetting.transform.DOLocalMoveX(50, 1.2f); //50
     
     debugSetting.transform.DOLocalMoveX(300, 1.2f); //300
 }

 public void SettingOffMove()
 {
     setting.transform.DOLocalMoveX(2500, 1f);
     soundSetting.transform.DOLocalMoveX(500, 1.2f);
     masterSetting.transform.DOLocalMoveX(800, 1.4f);
     vfxSetting.transform.DOLocalMoveX(1000, 1.45f);
     bgmSetting.transform.DOLocalMoveX(1200, 1.6f);
     
     mouseSetting.transform.DOLocalMoveX(500, 1.2f);
     
     debugSetting.transform.DOLocalMoveX(500, 1.2f);
 }

 private void Update()
 {
     if (Keyboard.current.escapeKey.wasPressedThisFrame)
     {
         SettingOffMove();
     }
 }


 //아무래도 난 노가다 체질인가... 이게 레전드 하드 코딩이지 뭐긴 뭐야

}
