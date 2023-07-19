using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HLProject
{
    public class UINetworkErrorPanel : CachedRectTransform
    {
        public void ShowErrorPanel()
        {
            ToggleErrorPanel(true);
        }

        public void ToggleErrorPanel(bool toggle)
        {
            MyRectTransform.localScale = toggle ? Vector3.one : Vector3.zero;
        }
    }
}
