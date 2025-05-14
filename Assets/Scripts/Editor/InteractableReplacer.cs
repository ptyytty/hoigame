#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class InteractableReplacer : MonoBehaviour
{
    [MenuItem("Tool/Replace Interactable Prefab")]
    static void ReplaceWithPrefab(){
        //교체할 프리팹 경로
        string prefabPath = "Assets/Prefabs_Object/Interactables/water purifier.prefab";
        //string prefabPath = "Assets/Prefabs_Map/DownStairs.prefab";
    
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if(prefab == null){
            Debug.Log("프리팹을 찾을 수 없습니다: " + prefabPath);
            return;
        }

        foreach (GameObject obj in Selection.gameObjects){
            GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            newObj.transform.position = obj.transform.position;
            newObj.transform.rotation = obj.transform.rotation;
            newObj.transform.localScale = obj.transform.localScale;

            Undo.RegisterCreatedObjectUndo(newObj, "Replace with Prefab");
            Undo.DestroyObjectImmediate(obj);
        }

        Debug.Log("선택된 오브젝트를 프리팹으로 교체 완료!");
    }
}
#endif