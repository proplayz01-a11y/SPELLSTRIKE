using UnityEngine;
using UnityEngine.UI;

/// <summary>Reusable, noninteractive world-space presentation for the active boss.</summary>
[DisallowMultipleComponent]
public sealed class BossOverheadHealthBar : MonoBehaviour
{
    private const float FrameWidth = 209f;
    private const float FrameHeight = 44f;
    private EnemyHealth target;
    private PersistentHUDRoot owner;
    private GameObject presentation;
    private RectTransform canvasRect;
    private Image fill;
    private Transform head;
    private Vector3 fallbackOffset;
    private float clearance;
    private bool anchorReady;
    public bool HasPresentation => presentation != null;

    public bool Show(EnemyHealth health, PersistentHUDRoot hud)
    {
        if (health == null || health.IsDead) return false;
        if (presentation == null && !Build()) return false;
        if (target != health)
        {
            target = health;
            anchorReady = false;
        }
        owner = hud;
        enabled = true;
        fill.fillAmount = health.HealthNormalized;
        return true;
    }

    public void Hide()
    {
        target = null;
        anchorReady = false;
        if (presentation != null) presentation.SetActive(false);
        enabled = false;
    }

    private bool Build()
    {
        Sprite frame = Resources.Load<Sprite>("SpellstrikeHUD/BossHealthBar_Frame");
        Sprite red = Resources.Load<Sprite>("SpellstrikeHUD/BossHealthBar_Fill");
        if (frame == null || red == null)
        {
            Debug.LogWarning("[BossOverheadHP] Missing imported boss sprites; using legacy HUD fallback.");
            return false;
        }
        presentation = new GameObject("BossOverheadHP", typeof(RectTransform), typeof(Canvas));
        canvasRect = (RectTransform)presentation.transform;
        canvasRect.sizeDelta = new Vector2(FrameWidth, FrameHeight);
        var canvas = presentation.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        // Exact registration from the approved sprite package; the navy track
        // is baked in the frame, while the shaped fill is independently clipped.
        CreateImage("BossHealthBar_Frame", frame, new Vector2(FrameWidth, FrameHeight), Vector2.zero);
        fill = CreateImage("BossHealthBar_Fill", red, new Vector2(169.4f, 14.2f), new Vector2(.1f, -2.6f));
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        presentation.SetActive(false);
        return true;
    }

    private Image CreateImage(string objectName, Sprite sprite, Vector2 size, Vector2 offset)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = (RectTransform)go.transform;
        rect.SetParent(canvasRect, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = offset;
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private void CaptureAnchor()
    {
        head = null;
        foreach (var animator in target.GetComponentsInChildren<Animator>(true))
        {
            if (!animator.isHuman || animator.avatar == null || !animator.avatar.isValid) continue;
            head = animator.GetBoneTransform(HumanBodyBones.Head);
            if (head != null) break;
        }
        // Prefer the actual skeletal head. For nonhumanoid models use the
        // resting mesh bounds, excluding unrelated arena and particle objects.
        Bounds bounds = new Bounds(target.transform.position, Vector3.zero);
        bool found = false;
        foreach (var mesh in target.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (!mesh.enabled || mesh.bounds.size.y < .01f) continue;
            if (!found) bounds = mesh.bounds; else bounds.Encapsulate(mesh.bounds);
            found = true;
        }
        if (!found)
            foreach (var collider in target.GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled || collider.isTrigger) continue;
                if (!found) bounds = collider.bounds; else bounds.Encapsulate(collider.bounds);
                found = true;
            }
        float height = found ? Mathf.Max(1f, bounds.size.y) : 2f;
        clearance = Mathf.Max(.25f, height * .10f);
        if (head != null)
            clearance = Mathf.Max(clearance, found ? bounds.max.y - head.position.y + height * .04f : clearance);
        Vector3 top = found ? new Vector3(bounds.center.x, bounds.max.y, bounds.center.z)
            : target.transform.position + Vector3.up * height;
        fallbackOffset = target.transform.InverseTransformPoint(top);
        anchorReady = true;
    }

    private void LateUpdate()
    {
        if (presentation == null) return;
        if (target == null || target.IsDead || target.currentHealth <= 0f)
        {
            Hide();
            return;
        }
        var cam = Camera.main;
        var titan = target.GetComponent<TitanEarthbreakerController>();
        var hollow = target.GetComponent<HollowKnightController>();
        bool canShow = target.isActiveAndEnabled && owner != null && owner.GameplayHealthVisible
            && cam != null && (titan == null || titan.IsHealthBarUnlocked)
            && (hollow == null || hollow.battleStarted);
        if (!canShow)
        {
            presentation.SetActive(false);
            return;
        }
        if (!anchorReady) CaptureAnchor();
        Vector3 anchor = (head != null ? head.position : target.transform.TransformPoint(fallbackOffset))
            + Vector3.up * clearance;
        Vector3 viewport = cam.WorldToViewportPoint(anchor);
        bool visible = viewport.z > cam.nearClipPlane && viewport.x > .035f && viewport.x < .965f
            && viewport.y > .06f && viewport.y < .96f && viewport.z < 220f;
        presentation.SetActive(visible);
        fill.fillAmount = target.HealthNormalized;
        if (!visible) return;
        canvasRect.position = anchor;
        canvasRect.rotation = cam.transform.rotation;
        float pixels = Mathf.Clamp(Screen.height * .155f, 100f, 185f);
        float viewHeight = cam.orthographic ? 2f * cam.orthographicSize
            : 2f * viewport.z * Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * .5f);
        canvasRect.localScale = Vector3.one * Mathf.Clamp(
            viewHeight * pixels / Mathf.Max(1, cam.pixelHeight) / FrameWidth, .0005f, .35f);
    }

    private void OnDisable()
    {
        if (presentation != null) presentation.SetActive(false);
    }

    private void OnDestroy()
    {
        if (presentation != null) Destroy(presentation);
    }
}
