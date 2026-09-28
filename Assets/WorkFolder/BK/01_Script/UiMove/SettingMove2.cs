using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class SettingMove2 : MonoBehaviour
{
    [SerializeField]private GameObject setting;
    [SerializeField]private GameObject soundSetting;
    [SerializeField]private GameObject mouseSetting;
    [SerializeField]private GameObject debugSetting;
    [SerializeField]private GameObject mikeSetting;
    [SerializeField]private GameObject closeBtn;
   private void Start()
   {
       setting.transform.DOLocalMoveX(2500, 0.0001f);
   }

   public void SettingOnMoves()
   {
      setting.transform.DOLocalMoveX(0, 0.7f).SetEase(Ease.OutBounce);
   }
   public void SettingOffMoves()
   {
       setting.transform.DOLocalMoveX(2500, 0.7f).SetEase(Ease.OutBounce);
   }
}
