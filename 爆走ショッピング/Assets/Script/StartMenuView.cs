using System;
using UnityEngine;
using UnityEngine.UI;

// 手作りまたは Prefab 化したスタートメニュー UI の参照置き場です。
// 役割:
// - StartMenuManager が必要とする Canvas、メニュー行、オプション表示領域への参照をまとめます。
// - UI の見た目は Prefab 側で作り、操作ロジックは StartMenuManager 側に置くための橋渡しです。
// 接続:
// - StartMenuManager.TryCreatePrefabUI が HasRequiredReferences を確認し、足りなければ実行時生成 UI にフォールバックします。
// 読むときの要点:
// - MenuRowReference は 1 行ぶんの root、background、labelText をまとめた小さな参照クラスです。
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

        // StartMenuManager 側からメニュー行の表示文字を設定します。
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
