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
        public Sprite frontHeroImage;
        public Sprite backHeroImage;
        public Sprite selectedImage;
        public Sprite deactivationImage;
    }
}
