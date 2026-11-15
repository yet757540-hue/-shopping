using System;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.EventSystems;

public class StartMenuControlManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private SceneController sceneController;
    [SerializeField] private GameObject Titles;
    [SerializeField] private GameObject Buttons;
    [SerializeField] private GameObject StartButton;
    [SerializeField] private GameObject TitleButton;

    private void Start()
    {
        Titles.SetActive(true);
        Buttons.SetActive(false);
        EventSystem.current.SetSelectedGameObject(TitleButton);
    }
    public void Start_Pressed()
    {
        Titles.SetActive(false);
        Buttons.SetActive(true);
        EventSystem.current.SetSelectedGameObject(StartButton);
    }
    public void CallMoveScene()
    {
        sceneController.MoveScene("TESTSCENE");
    }
}
