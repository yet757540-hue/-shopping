using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreboardManager : MonoBehaviour
{
    [Serializable]
    private class ScoreEntry
    {
        public ScoreTarget target;
        public int score;
    }

    [Header("References")]
    [SerializeField] private ImpactSettings impactSettings;
    [SerializeField] private TMP_Text scoreboardText;
    [SerializeField] private ScoreTarget[] targetPool;

    [Header("Board Settings")]
    [SerializeField] private int targetCount = 3;
    [SerializeField] private int targetScore = 100;
    [SerializeField] private bool clearWhenTimerStops = false;

    [Header("Score From Impact")]
    [SerializeField] private int minScoreGain = 5;
    [SerializeField] private int maxScoreGain = 35;

    [Header("Runtime UI")]
    [SerializeField] private bool createTextIfMissing = true;
    [SerializeField] private Vector2 anchoredPosition = new Vector2(-32f, 32f);
    [SerializeField] private Vector2 size = new Vector2(360f, 150f);
    [SerializeField] private int fontSize = 28;

    [Header("Incomplete Warning")]
    [SerializeField] private Color incompleteFlashColor = Color.red;
    [SerializeField] private int incompleteFlashCount = 3;
    [SerializeField] private float incompleteFlashInterval = 0.12f;

    private readonly List<ScoreEntry> activeEntries = new List<ScoreEntry>();
    private readonly StringBuilder textBuilder = new StringBuilder();
    private Color normalTextColor = Color.white;
    private Coroutine incompleteFlashCoroutine;
    private bool isBoardActive = false;

    public bool IsComplete
    {
        get
        {
            if (!isBoardActive || activeEntries.Count == 0)
            {
                return false;
            }

            foreach (ScoreEntry entry in activeEntries)
            {
                if (entry == null || entry.target == null || entry.score < targetScore)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public bool IsActive => isBoardActive && activeEntries.Count > 0;

    private void Awake()
    {
        if (impactSettings == null)
        {
            impactSettings = FindAnyObjectByType<ImpactSettings>();
        }

        if (impactSettings == null)
        {
            impactSettings = gameObject.AddComponent<ImpactSettings>();
        }

        if (scoreboardText == null && createTextIfMissing)
        {
            CreateRuntimeScoreboard();
        }

        if (scoreboardText != null)
        {
            normalTextColor = scoreboardText.color;
        }

        ClearScoreboard();
    }

    public void StartScoreboard()
    {
        if (isBoardActive)
        {
            return;
        }

        EnsureTargetPool();
        RestoreActiveTargetHighlights();

        activeEntries.Clear();
        isBoardActive = true;

        if (targetPool == null || targetPool.Length == 0)
        {
            RefreshText();
            return;
        }

        List<ScoreTarget> candidates = new List<ScoreTarget>();

        foreach (ScoreTarget target in targetPool)
        {
            if (target != null && !candidates.Contains(target))
            {
                candidates.Add(target);
            }
        }

        int count = Mathf.Min(targetCount, candidates.Count);

        for (int i = 0; i < count; i++)
        {
            int index = UnityEngine.Random.Range(0, candidates.Count);

            activeEntries.Add(new ScoreEntry
            {
                target = candidates[index],
                score = 0
            });

            candidates[index].SetHighlighted(true);

            candidates.RemoveAt(index);
        }

        RefreshText();
    }

    public bool TryCompleteScoreboard()
    {
        if (IsActive && !IsComplete)
        {
            FlashIncompleteWarning();
            return false;
        }

        CompleteScoreboard();
        return true;
    }

    public void StopScoreboard()
    {
        TryCompleteScoreboard();
    }

    private void CompleteScoreboard()
    {
        RestoreActiveTargetHighlights();
        isBoardActive = false;

        if (clearWhenTimerStops)
        {
            ClearScoreboard();
        }
    }

    public void ClearScoreboard()
    {
        RestoreActiveTargetHighlights();
        StopIncompleteFlash();

        isBoardActive = false;
        activeEntries.Clear();
        RefreshText();
    }

    public void RegisterCollision(Collision collision)
    {
        if (!isBoardActive || collision == null)
        {
            return;
        }

        ScoreTarget target = FindScoreTarget(collision);

        if (target == null)
        {
            return;
        }

        ScoreEntry entry = activeEntries.Find(item => item.target == target);

        if (entry == null || entry.score >= targetScore)
        {
            return;
        }

        int scoreGain = CalculateScoreGain(collision.relativeVelocity.magnitude);

        if (scoreGain <= 0)
        {
            return;
        }

        entry.score = Mathf.Min(targetScore, entry.score + scoreGain);

        if (entry.score >= targetScore && entry.target != null)
        {
            entry.target.SetHighlighted(false);
        }

        RefreshText();
    }

    public void FlashIncompleteWarning()
    {
        if (scoreboardText == null)
        {
            return;
        }

        StopIncompleteFlash();
        incompleteFlashCoroutine = StartCoroutine(FlashIncompleteCoroutine());
    }

    private ScoreTarget FindScoreTarget(Collision collision)
    {
        ScoreTarget target = null;

        if (collision.collider != null)
        {
            target = collision.collider.GetComponentInParent<ScoreTarget>();

            if (target != null)
            {
                return target;
            }
        }

        if (collision.rigidbody != null)
        {
            target = collision.rigidbody.GetComponentInParent<ScoreTarget>();
        }

        return target;
    }

    private int CalculateScoreGain(float impactSpeed)
    {
        if (impactSettings == null || !impactSettings.IsStrongEnough(impactSpeed))
        {
            return 0;
        }

        float impactRate = impactSettings.GetImpactRate(impactSpeed);

        return Mathf.RoundToInt(Mathf.Lerp(minScoreGain, maxScoreGain, impactRate));
    }

    private void EnsureTargetPool()
    {
        if (targetPool != null && targetPool.Length > 0)
        {
            return;
        }

        targetPool = FindObjectsByType<ScoreTarget>();
    }

    private void RefreshText()
    {
        if (scoreboardText == null)
        {
            return;
        }

        if (!isBoardActive || activeEntries.Count == 0)
        {
            scoreboardText.text = string.Empty;
            return;
        }

        textBuilder.Clear();

        foreach (ScoreEntry entry in activeEntries)
        {
            if (entry.target == null)
            {
                continue;
            }

            textBuilder
                .Append(entry.target.DisplayName)
                .Append(" (")
                .Append(entry.score)
                .Append("/")
                .Append(targetScore)
                .AppendLine(")");
        }

        scoreboardText.text = textBuilder.ToString().TrimEnd();
    }

    private void RestoreActiveTargetHighlights()
    {
        foreach (ScoreEntry entry in activeEntries)
        {
            if (entry != null && entry.target != null)
            {
                entry.target.SetHighlighted(false);
            }
        }
    }

    private IEnumerator FlashIncompleteCoroutine()
    {
        int count = Mathf.Max(1, incompleteFlashCount);
        float interval = Mathf.Max(0.01f, incompleteFlashInterval);

        for (int i = 0; i < count; i++)
        {
            scoreboardText.color = incompleteFlashColor;
            yield return new WaitForSeconds(interval);

            scoreboardText.color = normalTextColor;
            yield return new WaitForSeconds(interval);
        }

        scoreboardText.color = normalTextColor;
        incompleteFlashCoroutine = null;
    }

    private void StopIncompleteFlash()
    {
        if (incompleteFlashCoroutine != null)
        {
            StopCoroutine(incompleteFlashCoroutine);
            incompleteFlashCoroutine = null;
        }

        if (scoreboardText != null)
        {
            scoreboardText.color = normalTextColor;
        }
    }

    private void CreateRuntimeScoreboard()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Scoreboard Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject panelObject = new GameObject("Scoreboard Panel");
        panelObject.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = anchoredPosition;
        panelRect.sizeDelta = size;

        Image background = panelObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.45f);

        GameObject textObject = new GameObject("Scoreboard Text");
        textObject.transform.SetParent(panelObject.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 12f);
        textRect.offsetMax = new Vector2(-16f, -12f);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.BottomRight;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.enableWordWrapping = false;

        scoreboardText = text;
    }

    private void OnValidate()
    {
        targetCount = Mathf.Max(1, targetCount);
        targetScore = Mathf.Max(1, targetScore);
        minScoreGain = Mathf.Max(0, minScoreGain);
        maxScoreGain = Mathf.Max(minScoreGain, maxScoreGain);
        incompleteFlashCount = Mathf.Max(1, incompleteFlashCount);
        incompleteFlashInterval = Mathf.Max(0.01f, incompleteFlashInterval);
        size.x = Mathf.Max(120f, size.x);
        size.y = Mathf.Max(60f, size.y);
        fontSize = Mathf.Max(8, fontSize);
    }
}
