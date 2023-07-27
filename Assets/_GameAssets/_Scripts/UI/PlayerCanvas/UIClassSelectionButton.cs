using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using HLProject.Scriptables;

namespace HLProject
{
    public class UIClassSelectionButton : CachedRectTransform, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] Image imgClassIcon;
        [SerializeField] Button btnClassSelection;

        public int ClassIndex { get; private set; }

        bool pointerOverButton, showinName;
        float timeToShowClassName;
        string className;

        public System.Action OnHideName;
        public System.Action<string, Vector3> OnShowName;

        public void Init(TeamClassData classData, int classIndex, System.Action<int> OnButtonClick)
        {
            imgClassIcon.sprite = classData.classSprite;
            className = classData.className;
            ClassIndex = classIndex;
            btnClassSelection.onClick.AddListener(() => { OnButtonClick?.Invoke(classIndex); });
        }

        void Update()
        {
            if (showinName || !pointerOverButton || Time.time < timeToShowClassName) return;
            OnShowName?.Invoke(className, new Vector3(MyRectTransform.position.x + 50, MyRectTransform.position.y + 25, MyRectTransform.position.z));
            showinName = true;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            showinName = false;
            pointerOverButton = true;
            timeToShowClassName = Time.time + .8f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            showinName = false;
            pointerOverButton = false;
            OnHideName?.Invoke();
        }

        public void ToggleSelection(bool toggle)
        {
            btnClassSelection.image.color = toggle ? btnClassSelection.colors.pressedColor : btnClassSelection.colors.normalColor;
        }
    }
}
