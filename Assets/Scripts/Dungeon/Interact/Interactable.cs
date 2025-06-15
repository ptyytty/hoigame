using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 상호작용 오브젝트 자동 부착
public class Interactable : MonoBehaviour
{

    void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Interactable");
        Debug.Log($"{gameObject.name}은 상호작용 가능 오브젝트입니다.");
    }

    public void ShowUI(bool show)
    {
        InteractableManager.instance.interactionUI.SetActive(show);
    }
}
