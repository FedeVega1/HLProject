using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HLProject
{
    interface IOptionTab
    {
        public void Init();
        public void ToggleTab(bool toggle);
        public string TabType { get; set; }
    }

    public class UIOptions : CachedRectTransform
    {
        static readonly Color32 pressedColor = new Color32(0xC8, 0xC8, 0xC8, 0xFF);

        [SerializeField] RectTransform tabsPivot, topButtonsPivot;

        int currentTab = -1;
        List<IOptionTab> optionTabs;
        List<Image> tabButtons;

        void Start()
        {
            optionTabs = new List<IOptionTab>();
            tabButtons = new List<Image>();

            int size = tabsPivot.childCount;
            for (int i = 0; i < size; i++)
                {
                IOptionTab tab = tabsPivot.GetChild(i).GetComponent<IOptionTab>();
                if (tab == null) continue;
                tab.Init();
                optionTabs.Add(tab);
            }

            size = topButtonsPivot.childCount;
            for (int i = 0; i < size; i++)
            {
                Image btnTab = topButtonsPivot.GetChild(i).GetComponent<Image>();
                if (btnTab == null) continue;
                tabButtons.Add(btnTab);
            }

            currentTab = 0;
            optionTabs[currentTab].ToggleTab(true);
            tabButtons[currentTab].color = pressedColor;
        }

        public void OpenTab(string tabType)
        {
            if (currentTab != -1)
            {
                optionTabs[currentTab].ToggleTab(false);
                tabButtons[currentTab].color = Color.white;
            }

            int size = optionTabs.Count;
            for (int i = 0; i < size; i++)
            {
                if (optionTabs[i].TabType == tabType)
                {
                    currentTab = i;
                    optionTabs[currentTab].ToggleTab(true);
                    tabButtons[i].color = pressedColor;
                    return;
                }
            }
        }

        public void TogglePanel(bool toggle)
        {
            MyRectTransform.localScale = toggle ? Vector3.one : Vector3.zero;
        }
    }
}
