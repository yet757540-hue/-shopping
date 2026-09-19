using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>事前作成したタイトル画面 UI を StartMenuManager へ渡す参照コンテナです。</summary>
public class StartMenuView : MonoBehaviour
{
    // 事前作成した一行分の配置・背景・文字への参照をまとめます。
    [Serializable]
    public sealed class MenuRowReference
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private Image background;
        [SerializeField] private Text labelText;

        // このメニュー行の配置先を公開します。
        public RectTransform Root => root;
        // 選択色や編集色を反映する背景画像を公開します。
        public Image Background => background;
        // この行の名前を表示する Text を公開します。
        public Text LabelText => labelText;
        // この行を表示するための必須参照がそろっているかを返します。
        public bool IsValid => root != null && background != null && labelText != null;

        // ラベル参照が有効な場合だけ、メニュー行の表示文字を差し替えます。
        public void SetLabel(string label)
        {
            if (labelText != null)
            {
                labelText.text = label;
            }
        }
    }

    // 事前作成した画面全体の Canvas と、メインメニューの配置先です。
    [Header("ルート")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform mainMenuRoot;

    // 開始・オプション・終了の各行への参照です。
    [Header("メインメニュー行")]
    [SerializeField] private MenuRowReference startRow;
    [SerializeField] private MenuRowReference optionRow;
    [SerializeField] private MenuRowReference exitRow;

    // オプション全体と項目配置先、戻る案内の参照です。
    [Header("オプションポップアップ")]
    [SerializeField] private GameObject optionPopupRoot;
    [SerializeField] private RectTransform optionContentRoot;
    [SerializeField] private Text optionBackHint;

    // 事前作成ビューの描画先 Canvas を公開します。
    public Canvas Canvas => canvas;
    // メインメニュー全体の配置と表示に使うルートを公開します。
    public RectTransform MainMenuRoot => mainMenuRoot;
    // オプション全体の表示切り替えに使うルートを公開します。
    public GameObject OptionPopupRoot => optionPopupRoot;
    // オプション項目を配置する領域を公開します。
    public RectTransform OptionContentRoot => optionContentRoot;
    // 戻る操作の案内を表示する Text を公開します。
    public Text OptionBackHint => optionBackHint;
    // ゲーム開始を担当するメニュー行を公開します。
    public MenuRowReference StartRow => startRow;
    // オプションを開くメニュー行を公開します。
    public MenuRowReference OptionRow => optionRow;
    // 終了を担当するメニュー行を公開します。
    public MenuRowReference ExitRow => exitRow;

    // 画面全体と各行の必須参照がそろい、このビューを利用できるかを返します。
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
