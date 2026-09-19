using UnityEngine;
using UnityEngine.UI;

// RawImage の UV を動かして、フィルムの穴だけを流します。
// 帯そのものは角に固定したままなので、位置を保ったまま回転しているように見えます。
[RequireComponent(typeof(RawImage))]
public sealed class FilmStripScroller : MonoBehaviour
{
    private RawImage target;
    private float tileCount = 1f;
    private float pitch = 75f;
    private float speed = 340f;
    private float direction = 1f;
    private float offset;

    public void Initialize(RawImage image, float tiles, float pitchPixels, float scrollSpeed, float scrollDirection, float startPhase)
    {
        target = image;
        tileCount = Mathf.Max(0.001f, tiles);
        pitch = Mathf.Max(1f, pitchPixels);
        speed = scrollSpeed;
        direction = scrollDirection >= 0f ? 1f : -1f;
        offset = Mathf.Repeat(startPhase, 1f);
        Apply();
    }

    private void Update()
    {
        if (target == null)
        {
            return;
        }

        offset = Mathf.Repeat(offset + direction * speed * Time.unscaledDeltaTime / pitch, 1f);
        Apply();
    }

    private void Apply()
    {
        if (target != null)
        {
            target.uvRect = new Rect(offset, 0f, tileCount, 1f);
        }
    }
}
