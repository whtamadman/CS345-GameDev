using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RangeModifierTracker : MonoBehaviour
{
    public static float rangeModifier = 0;
    private TMP_Text score;
    public void Start()
    {
        score = GetComponent<TMP_Text>();
        if(score==null)
            Debug.Log("null reference!!!");
    }
 
    public void Update()
    {
        rangeModifier = Player.Instance.baseRangeModifier;
        score.text = "+ " + rangeModifier.ToString();
    }
}