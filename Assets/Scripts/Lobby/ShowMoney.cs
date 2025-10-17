using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShowMoney : MonoBehaviour
{
    public static ShowMoney intance { get; private set; }

    [Header("Player Money")]
    [SerializeField] private TestMoney ownedMoney;

    [Header("Show Current Money")]
    [SerializeField] private TMP_Text coin;
    [SerializeField] private TMP_Text redSoul;
    [SerializeField] private TMP_Text blueSoul;
    [SerializeField] private TMP_Text greenSoul;

    void Start()
    {
        
    }

    void Update()
    {
        UpdateOwnedMoney();
    }

    public void UpdateOwnedMoney()
    {
        coin.text = ownedMoney.money.ToString();
        redSoul.text = ownedMoney.redSoul.ToString();
        blueSoul.text = ownedMoney.blueSoul.ToString();
        greenSoul.text = ownedMoney.greenSoul.ToString();
    }
}
