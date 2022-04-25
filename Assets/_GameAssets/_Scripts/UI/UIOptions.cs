using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIOptions : CachedRectTransform
{
    public void Toggle(bool toggle)
    {
        MyRectTransform.localScale = toggle ? Vector3.one : Vector3.zero;
    }
}
