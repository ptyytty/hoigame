using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    void Start()
    {
        EnableInteraction();
    }
    public void EnableInteraction(){
        gameObject.layer = LayerMask.NameToLayer("Interactable");
        Debug.Log($"{gameObject.name}은 상호작용 가능 오브젝트입니다.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Party")){
            Debug.Log($"{name} 상호작용!");
        }
    }
}
