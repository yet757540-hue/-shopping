using System;
using UnityEngine;
using UnityEngine.UI;

public class StartMenuView : MonoBehaviour
{
    [Serializable]
    public sealed class MenuRowReference
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private Image background;
        [SerializeField] private Text labelText;

        public RectTransform Root => root;
        public Image Background => background;
        public Text LabelText => labelText;
        public bool IsValid => root != null && background != null && labelText != null;

        public void SetLabel(string label)
        {
            if (labelText != null)
            {
                labelText.text = label;
            }
        }
    }

    [Header("Root")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform mainMenuRoot;

    [Header("Main Menu Rows")]
    [SerializeField] private MenuRowReference startRow;
    [SerializeField] private MenuRowReference optionRow;
    [SerializeField] private MenuRowReference exitRow;

    [Header("Option Popup")]
    [SerializeField] private GameObject optionPopupRoot;
    [SerializeField] private RectTransform optionContentRoot;
    [SerializeField] private Text optionBackHint;

    public Canvas Canvas => canvas;
    public RectTransform MainMenuRoot => mainMenuRoot;
    public GameObject OptionPopupRoot => optionPopupRoot;
    public RectTransform OptionContentRoot => optionContentRoot;
    public Text OptionBackHint => optionBackHint;
    public MenuRowReference StartRow => startRow;
    public MenuRowReference OptionRow => optionRow;
    public MenuRowReference ExitRow => exitRow;

    public bool HasRequiredReferences =>
        canvas != null &&
        mainMenuRoot != null &&
        optionPopupRoot != null &&
        optionContentRoot != null &&
        optionBackHint != null &&
        startRow != null &&
        startRow.IsValid &&
        optionRow != null &&
        optionRow.IsValid &&
        exitRow != null &&
        exitRow.IsValid;
}
