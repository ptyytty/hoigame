using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InteractionScanner : MonoBehaviour
{
    public float detectionDistance = 30f;
    public LayerMask interactableLayer;
    public GameObject interactionUI;
    private Interactable currentTarget;
    private DungeonManager dungeonManager;

    void Update()
    {
        MoveDirection currentMoveDirection = dungeonManager.currentDir;
        ScanForInteractable();
    }

    void ScanForInteractable(){
        Vector3 origin = transform.position + Vector3.up * 1.2f;
        Vector3 rayDirection = GetDiagonalDirection();
        Ray ray = new Ray(origin, rayDirection * detectionDistance);
        Debug.DrawRay(origin, rayDirection * detectionDistance, Color.green); // 시각 디버그

        RaycastHit hit;

        if(Physics.Raycast(ray, out hit, detectionDistance)){
            Interactable interactable = hit.collider.GetComponent<Interactable>();

            if (interactable != null && interactable != currentTarget){
                currentTarget = interactable;
                ShowInteractionUI(true);
            }
        }else{
            if(currentTarget != null){
                currentTarget = null;
                ShowInteractionUI(false);
            }
        }
    }

    Vector3 GetDiagonalDirection(){
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        if(currentMoveDirection == MoveDirection.Left){
            return (Quaternion.AngleAxis(-30f, Vector3.up) * forward).normalized;
        }else{
            return (Quaternion.AngleAxis(30f, Vector3.up) * forward).normalized;
        }
    }

    void ShowInteractionUI(bool show){
        if(interactionUI != null)
            interactionUI.SetActive(show);
    }
}
