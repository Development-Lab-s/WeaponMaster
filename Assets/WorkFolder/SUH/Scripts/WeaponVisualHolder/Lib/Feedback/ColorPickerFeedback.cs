using DG.Tweening;
using UnityEngine;

namespace WorkFolder.SUH.Scripts.WeaponVisualHolder.Lib.Feedback
{
    public class ColorPickerFeedback : AbstractFeedback
    {
        [SerializeField] private Transform colorPickerObject;
        [SerializeField] private Transform showColorPicker_Pos;
        [SerializeField] private Transform hideColorPicker_Pos;

        private bool colorPickerState_Debounce;

        private Sequence _sequence;
        
        public override void CreateFeedback()
        {
            #region ColorPicker_Pos Checker

            if (colorPickerObject.position == showColorPicker_Pos.position)
                colorPickerState_Debounce = true;
            else
                colorPickerState_Debounce = false;
            
            #endregion
            
            #region ShowColorPickerFeedback
            
            if (colorPickerState_Debounce)
            {
                _sequence = DOTween.Sequence();

                _sequence.Append(colorPickerObject.DOMove(showColorPicker_Pos.position, .5f).SetEase(Ease.InFlash))
                    .Append(colorPickerObject.DORotate(hideColorPicker_Pos.position, .5f).SetEase(Ease.InFlash));
                _sequence = null;
            }
            
            #endregion
            
            #region HideColorPickerFeedback
            
            else
            {
                _sequence = DOTween.Sequence();
                _sequence.Append(colorPickerObject.DOMove(hideColorPicker_Pos.position, .5f).SetEase(Ease.InFlash))
                    .Append(colorPickerObject.DORotate(hideColorPicker_Pos.position, .5f).SetEase(Ease.InFlash));
                _sequence = null;
            }
            
            #endregion
        }

        public override void FinishFeedback()
        {
        }
    }
}