using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 상호작용 오브젝트 자동 부착
public class Interactable : MonoBehaviour
{
    OutlineDuplicator _outline;

    void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Interactable");
        //Debug.Log($"{gameObject.name}은 상호작용 가능 오브젝트입니다.");

        InteractableManager.instance.interactionUp.onClick.RemoveAllListeners();
        InteractableManager.instance.interactionDown.onClick.RemoveAllListeners();

        InteractableManager.instance.interactionUp.onClick.AddListener(() =>
        {
            Vector3 pos = InteractableManager.instance.party.transform.position;
            pos.x -= 150f;
            InteractableManager.instance.party.transform.position = pos;

            Vector3 posCam = InteractableManager.instance.partyCam.transform.position;
            posCam.x -= 150f;
            InteractableManager.instance.partyCam.transform.position = posCam;
        });

        InteractableManager.instance.interactionDown.onClick.AddListener(() =>
        {
            Vector3 pos = InteractableManager.instance.party.transform.position;
            pos.x += 150f;
            InteractableManager.instance.party.transform.position = pos;

            Vector3 posCam = InteractableManager.instance.partyCam.transform.position;
            posCam.x += 150f;
            InteractableManager.instance.partyCam.transform.position = posCam;
        });

        TryGetComponent(out _outline);
    }

    public void ShowUI(bool show)
    {
        if (gameObject.CompareTag("Upstair"))
            InteractableManager.instance.interactionUp.gameObject.SetActive(show);
        else if (gameObject.CompareTag("Downstair"))
            InteractableManager.instance.interactionDown.gameObject.SetActive(show);

        else if (gameObject.CompareTag("Untagged"))
            InteractableManager.instance.interactionObj.gameObject.SetActive(show);
        // 오브젝트 UI

        if (_outline != null) _outline.EnableOutline(show);
    }
}
