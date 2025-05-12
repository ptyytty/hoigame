#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections.Generic;

public class InteractableList : MonoBehaviour
{
    [SerializeField] private Transform parentObject; // 상호작용 오브젝트 그룹의 부모
    [SerializeField] private string[] nameFilters = { "Cabinet", "vending machine V2", "water purifier" };

    public List<GameObject> candidates = new List<GameObject>();

    [ContextMenu("Scan Children by Name and Set Candidates")]
    public void ScanChildrenByNameAndSetCandidates()
    {
#if UNITY_EDITOR
        if (parentObject == null)
        {
            Debug.LogError("❌ parentObject가 설정되지 않았습니다.");
            return;
        }

        candidates.Clear();
        int addedCount = 0;

        foreach (Transform child in parentObject)
        {
            string objName = child.name.ToLower();

            foreach (string keyword in nameFilters)
            {
                if (objName.Contains(keyword.ToLower()))
                {
                    GameObject obj = child.gameObject;
                    candidates.Add(obj);
                    obj.tag = "InteractableCandidate"; // ✅ 태그 일괄 설정
                    Debug.Log($"✔️ 이름 '{obj.name}' 등록됨");
                    addedCount++;
                    break;
                }
            }
        }

        EditorUtility.SetDirty(this);
        Debug.Log($"✅ 이름 필터 기준 후보 등록 완료: {addedCount}개");
#endif
    }
}
