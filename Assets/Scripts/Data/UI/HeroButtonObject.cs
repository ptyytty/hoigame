using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroButtonObject : MonoBehaviour
{
    [CreateAssetMenu(fileName = "UIAssets", menuName = "Game/Preparation Asset Collection")]
    public class ChangedImage : ScriptableObject
    {
        public Sprite defaultImage;
        public Sprite selectedImage;
        public Sprite deactivationImage;
    }

    public class HeroButton : ScriptableObject
    {
        public Button framePrefab;

    }

    public Image background;
    public Image heroImage;
    public TMP_Text heroName;
    public TMP_Text heroJob;
    public TMP_Text heroLevel;

    public Button button => GetComponent<Button>();
}
