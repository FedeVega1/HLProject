using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        [SerializeField] RectTransform tabsPivot;

        IOptionTab currentTab;
        List<IOptionTab> optionTabs;

        void Start()
        {
            optionTabs = new List<IOptionTab>();

            foreach (Transform child in tabsPivot)
            {
                IOptionTab tab = child.GetComponent<IOptionTab>();
                if (tab == null) continue;
                tab.Init();
                optionTabs.Add(tab);
            }

            currentTab = optionTabs[0];
            currentTab.ToggleTab(true);
        }

        public void OpenTab(string tabType)
        {
            if (currentTab != null) currentTab.ToggleTab(false);

            int size = optionTabs.Count;
            for (int i = 0; i < size; i++)
            {
                if (optionTabs[i].TabType == tabType)
                {
                    currentTab = optionTabs[i];
                    currentTab.ToggleTab(true);
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
