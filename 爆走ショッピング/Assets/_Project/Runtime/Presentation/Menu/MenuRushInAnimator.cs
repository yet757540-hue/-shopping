using UnityEngine;

// 画面右の外から要素を飛び込ませ、少し行き過ぎてから収まる動きを作ります。
// 入り際に横へ潰すことで、勢いがあるように見せています。
[RequireComponent(typeof(RectTransform))]
public sealed class MenuRushInAnimator : MonoBehaviour
{
    private RectTransform target;
    private CanvasGroup group;
    private Vector2 settledPosition;
    private Vector3 settledScale = Vector3.one;
    private RushInStyle style = new RushInStyle();
    private float elapsed;
    private bool playing;
    private bool initialized;
    private bool hasCompleted;
    private float startOffset;

    public bool IsPlaying => playing;
    public RushInStyle Style => style;

    public void Initialize(RushInStyle rushStyle)
    {
        target = (RectTransform)transform;
        settledPosition = target.anchoredPosition;
        settledScale = target.localScale;
        style = rushStyle != null ? rushStyle.Clone() : new RushInStyle();
        initialized = true;

        if (style.fadeIn)
        {
            group = GetComponent<CanvasGroup>();

            if (group == null)
            {
                group = gameObject.AddComponent<CanvasGroup>();
            }
        }

        Play();
    }

    // レイアウト変更後に、収まった状態の位置と拡大率を再取得します。
    public void CaptureSettledTransform()
    {
        if (target == null)
        {
            target = (RectTransform)transform;
        }

        settledPosition = target.anchoredPosition;
        settledScale = target.localScale;
    }

    public void Play()
    {
        if (target == null)
        {
            target = (RectTransform)transform;
        }

        elapsed = 0f;
        playing = style.enabled;
        hasCompleted = false;
        startOffset = style.startOffsetX;

        if (style.startOutsideCanvas && target.parent is RectTransform parent)
        {
            // Resolve the viewport in the animated element's parent space, including
            // CanvasScaler and the element's pivot, rotation and initial stretch.
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                target.anchoredPosition = settledPosition;
                target.localScale = Vector3.Scale(settledScale,
                    new Vector3(style.startScaleX, style.startScaleY, 1f));
                Vector3[] corners = new Vector3[4];
                ((RectTransform)canvas.rootCanvas.transform).GetWorldCorners(corners);
                float right = float.NegativeInfinity;
                foreach (Vector3 corner in corners)
                    right = Mathf.Max(right, parent.InverseTransformPoint(corner).x);
                target.GetWorldCorners(corners);
                float left = float.PositiveInfinity;
                foreach (Vector3 corner in corners)
                    left = Mathf.Min(left, parent.InverseTransformPoint(corner).x);
                startOffset = Mathf.Max(startOffset, right - left + 32f);
            }
        }

        if (!playing)
        {
            CompleteNow();
            return;
        }

        Apply(0f);
    }

    public void CompleteNow()
    {
        playing = false;
        hasCompleted = true;
        Apply(1f);
    }

    private void OnEnable()
    {
        if (!initialized)
        {
            return;
        }

        // オプションを閉じて再表示したときに、突入演出をやり直さないようにします。
        // やり直すと次のフレームまで要素が画面外に残ってしまいます。
        if (hasCompleted)
        {
            Apply(1f);
            return;
        }

        if (!playing && style != null && style.enabled)
        {
            Play();
        }
    }

    private void Update()
    {
        if (!playing || style == null)
        {
            return;
        }

        elapsed += Time.unscaledDeltaTime;

        float duration = Mathf.Max(0.0001f, style.duration);
        float t = (elapsed - style.delay) / duration;

        if (t < 0f)
        {
            Apply(0f);
            return;
        }

        if (t >= 1f)
        {
            CompleteNow();
            return;
        }

        Apply(t);
    }

    private void Apply(float t)
    {
        float clamped = Mathf.Clamp01(t);
        float eased = EaseOutBack(clamped, style.overshoot);

        target.anchoredPosition = settledPosition + new Vector2(startOffset * (1f - eased), 0f);

        Vector3 scale = settledScale;
        scale.x *= Mathf.LerpUnclamped(style.startScaleX, 1f, eased);
        scale.y *= Mathf.LerpUnclamped(style.startScaleY, 1f, eased);
        target.localScale = scale;

        if (group != null)
        {
            if (!style.fadeIn)
            {
                group.alpha = 1f;
            }
            else
            {
                float fadeWindow = Mathf.Max(0.01f, style.fadePortion);
                group.alpha = Mathf.Lerp(style.startAlpha, 1f, Mathf.Clamp01(clamped / fadeWindow));
            }
        }
    }

    private static float EaseOutBack(float t, float overshoot)
    {
        if (t >= 1f)
        {
            return 1f;
        }

        float strength = Mathf.Max(0f, overshoot) * 5f;
        float c3 = strength + 1f;
        float p = t - 1f;
        return 1f + c3 * p * p * p + strength * p * p;
    }
}
