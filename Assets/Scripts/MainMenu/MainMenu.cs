using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
 
public class MainMenu : MonoBehaviour
{

    public GameObject TitlesAndButtons;
    public GameObject OptionMenu;


    void Start() {
        TitlesAndButtons.SetActive(true);
        OptionMenu.SetActive(false);
    }

    public void Play()
    {
        AudioManager.Instance.ButtonClick();
        SceneManager.LoadScene("Main Scene");
    }
 
    public void Quit()
    {
        AudioManager.Instance.ButtonClick();
        Application.Quit();
        EditorApplication.isPlaying = false;
    }

    public void Options() 
    {
        AudioManager.Instance.ButtonClick();
        TitlesAndButtons.SetActive(false);
        OptionMenu.SetActive(true);
    }

    public void Back() {
        AudioManager.Instance.ButtonClick();
        OptionMenu.SetActive(false);
        TitlesAndButtons.SetActive(true);
    }
}