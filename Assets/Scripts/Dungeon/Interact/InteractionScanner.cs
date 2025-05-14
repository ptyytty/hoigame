using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InteractionScanner : MonoBehaviour
{
    public float detectionDistance = 40f;   // 탐지 거리
    public GameObject interactionUI;
    [SerializeField] private Transform partyTransform;
    private Interactable currentTarget;


    void Update()
    {
        ScanForInteractable();
    }

    void ScanForInteractable(){
        Vector3 origin = transform.position + Vector3.up * 1.2f;    // Party 위치에서 Y 값 올리기
        Vector3 rayDirection = GetDiagonalDirection(DungeonManager.instance.currentDir);
        Ray ray = new Ray(origin, rayDirection * detectionDistance);
        Debug.DrawRay(origin, rayDirection * detectionDistance, Color.blue); // 시각 디버그

        RaycastHit hit;

        if(Physics.Raycast(ray, out hit, detectionDistance)){
            Interactable interactable = hit.collider.GetComponent<Interactable>();

            if (interactable != null && interactable != currentTarget){
                currentTarget = interactable;
                ShowInteractionUI(true);
            }
        }else{
            if (currentTarget != null){
                currentTarget = null;
                ShowInteractionUI(false);
            }
        }
    }

    Vector3 GetDiagonalDirection(MoveDirection dir){

        if(dir == MoveDirection.Left){
            return (Quaternion.AngleAxis(-120f, Vector3.up) * partyTransform.forward).normalized;
        }else{
            return (Quaternion.AngleAxis(120f, Vector3.up) * partyTransform.forward).normalized;
        }
    }

    void ShowInteractionUI(bool show){
        if(interactionUI != null)
            interactionUI.SetActive(show);
    }
}
