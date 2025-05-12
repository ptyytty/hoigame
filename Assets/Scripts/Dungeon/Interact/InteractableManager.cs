using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableManager : MonoBehaviour
{
    [SerializeField]private List<GameObject> candiates;
    [SerializeField] private float interactionChance = 0.1f;
    void Start()
    {
        AssingInteractables();
    }

    void AssingInteractables(){
        foreach(GameObject obj in candiates){
            if(Random.value <= interactionChance){
                if(!obj.TryGetComponent(out Interactable interactable)){
                    obj.AddComponent<Interactable>();
                    
                }
            }
        }
    }
}
