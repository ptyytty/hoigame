using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NaNScanner : MonoBehaviour
{
    void LateUpdate()
    {
        var all = FindObjectsOfType<RectTransform>();
        foreach (var rt in all)
        {
            if (float.IsNaN(rt.localPosition.x) || float.IsNaN(rt.localPosition.y))
            {
                Debug.LogError($"[NaNScanner] {rt.name} at path {GetPath(rt)} has NaN position!", rt);
            }
        }
    }

    string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
