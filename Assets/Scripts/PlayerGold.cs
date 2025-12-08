using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreDisplay : MonoBehaviour
{
    public static int coins = 0;
    private TMP_Text score;
    public void Start()
    {
        score = GetComponent<TMP_Text>();
        if(score==null)
            Debug.Log("null reference!!!");
    }
 
    public void Update()
    {
        coins = Player.Instance.coins;
        score.text = "X " + coins.ToString();
    }
}