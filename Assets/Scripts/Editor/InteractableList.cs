#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections.Generic;

public class InteractableList : MonoBehaviour
{
    [SerializeField] private Transform parentObject; // 상호작용 오브젝트 그룹의 부모
    [SerializeField] private string[] nameFilters = { "Cabinet", "vending machine V2", "water purifier" };
    [SerializeField] private string[] stairFilters = {"UpStairs", "DownStairs"};

    public List<GameObject> candidates = new List<GameObject>();
    public List<GameObject> stairs = new List<GameObject>();

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
        int candidateCount = 0;
        int stairCount = 0;

        foreach (Transform child in parentObject)
        {
            string objName = child.name.ToLower();

            foreach(string keyword in stairFilters)
            {
                if(objName.Contains(keyword.ToLower()))
                {
                    GameObject obj = child.gameObject;
                    stairs.Add(obj);
                    Debug.Log($"🔼 계단 '{obj.name}' 등록됨");
                    stairCount++;
                    break;
                }
            }

            foreach (string keyword in nameFilters)
            {
                if (objName.Contains(keyword.ToLower()))
                {
                    GameObject obj = child.gameObject;
                    candidates.Add(obj);
                    obj.tag = "InteractableCandidate"; // ✅ 태그 일괄 설정
                    Debug.Log($"✔️ 이름 '{obj.name}' 등록됨");
                    candidateCount++;
                    break;
                }
            }
        }

        EditorUtility.SetDirty(this);
        Debug.Log($"✅ 상호작용 후보 등록: {candidateCount}개");
        Debug.Log($"✅ 계단 오브젝트 등록: {stairCount}개");
#endif
    }

    [ContextMenu("Clear Interactable Lists")]
    public void ClearLists()
    {
#if UNITY_EDITOR
        candidates.Clear();
        stairs.Clear();
        Debug.Log("🧹 후보 리스트와 계단 리스트를 초기화했습니다.");
        EditorUtility.SetDirty(this);
#endif
    }
}
