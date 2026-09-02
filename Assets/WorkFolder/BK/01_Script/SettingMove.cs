using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;
public class SettingMove : MonoBehaviour
{
 [Header("UI")] [SerializeField] private GameObject ui;
 
 [Header("Background, title")]
 [SerializeField] private GameObject settingPanel;
 [SerializeField] private GameObject title;
 [Header("BTM")] 
 [SerializeField] private GameObject btms;
 [SerializeField] private GameObject mainMenuBTM;
 [SerializeField] private GameObject SettingOffBTM;

 [Header("BarUI")]
 [SerializeField] private GameObject bars;
 [SerializeField] private GameObject masterSoundBar;
 [SerializeField] private GameObject vfxSoundBar;
 [SerializeField] private GameObject bgmSoundBar;
 [SerializeField] private GameObject mouseBar;

 private void Start()
 {
     if(ui == null) return;
     ui.transform.DOLocalMoveX(2000, 0.001f);
     
    masterSoundBar.transform.DOLocalMoveX(-330, 0.001f);
    vfxSoundBar.transform.DOLocalMoveX(40, 0.001f);
    bgmSoundBar.transform.DOLocalMoveX(330, 0.001f);
    mouseBar.transform.DOLocalMoveX(590, 0.001f);
    
    /*mainMenuBTM.transform.DOLocalMoveX(620, 0.001f);
    SettingOffBTM.transform.DOLocalMoveX(830, 0.001f);*/
    btms.transform.DOLocalMoveX(630, 1.25f);
    
 }

 /*private IEnumerator Coroutine()
 {
     yield return new WaitForSeconds(2f);
     SettingMoves();
 }

 private void OnEnable()
 {
   StartCoroutine(Coroutine());
 }*/


 public void SettingMoves()
 {
     ui.transform.DOLocalMoveX(0, 1f);
     
     masterSoundBar.transform.DOLocalMoveX(0, 0.5f);
     vfxSoundBar.transform.DOLocalMoveX(0, 0.75f);
     bgmSoundBar.transform.DOLocalMoveX(0, 1f);
     mouseBar.transform.DOLocalMoveX(0, 1.25f);
    
     /*mainMenuBTM.transform.DOLocalMoveX( 0, 1f);
     SettingOffBTM.transform.DOLocalMoveX(0, 1.25f);*/
     btms.transform.DOLocalMoveX(-10, 1.25f);
 }

 public void SettingOffMove()
 {
     ui.transform.DOLocalMoveX(2000, 0.5f); //1
     masterSoundBar.transform.DOLocalMoveX(-330, 0.5f);
     vfxSoundBar.transform.DOLocalMoveX(40, 0.5f); //0.75
     bgmSoundBar.transform.DOLocalMoveX(330, 0.5f); //0.1
     mouseBar.transform.DOLocalMoveX(590, 0.5f); //1.25
    
     /*mainMenuBTM.transform.DOLocalMoveX(620, 1.25f);
     SettingOffBTM.transform.DOLocalMoveX(830, 1.5f);*/
     
     btms.transform.DOLocalMoveX(630, 0.5f); //1.25
     
 }


 public void ScaleUpSetting()
 {
     ui.transform.DOScale(1f, 1f);
 }

 public void ScaleDownSetting()
 {
     ui.transform.DOScale(0f, 0f);
 }

//아무래도 난 노가다 체질인가... 이게 레전드 하드 코딩이지 뭐긴 뭐야

}
