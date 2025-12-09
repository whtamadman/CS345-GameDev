using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MeleeModifierTracker : MonoBehaviour
{
    public static float meleeModifier = 0;
    private TMP_Text score;
    public void Start()
    {
        score = GetComponent<TMP_Text>();
        if(score==null)
            Debug.Log("null reference!!!");
    }
 
    public void Update()
    {
        meleeModifier = Player.Instance.damageMeleeModifier;
        score.text = "+ " + meleeModifier.ToString();
    }
}