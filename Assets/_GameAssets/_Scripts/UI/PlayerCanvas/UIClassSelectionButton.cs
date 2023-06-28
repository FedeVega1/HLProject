using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HLProject
{
    public class UIClassSelectionButton : MonoBehaviour
    {
        [SerializeField] Image imgClassIcon;
        [SerializeField] TMP_Text lblClassName;
        [SerializeField] Button btnClassSelection;

        public int ClassIndex { get; private set; }

        public void Init(TeamClassData classData, int classIndex, System.Action<int> OnButtonClick)
        {
            imgClassIcon.sprite = classData.classSprite;
            lblClassName.text = classData.className;
            btnClassSelection.onClick.AddListener(() => { OnButtonClick?.Invoke(classIndex); });
        }

        public void ToggleSelection(bool toggle)
        {
            btnClassSelection.image.color = toggle ? btnClassSelection.colors.pressedColor : btnClassSelection.colors.normalColor;
        }
    }
}
