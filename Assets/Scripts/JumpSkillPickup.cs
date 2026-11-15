using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
public sealed class JumpSkillPickup : MonoBehaviour
{
    [SerializeField] private Color pickupColor = new Color(1f, 0.78f, 0.12f, 1f);
    [SerializeField] private Vector2 pickupSize = new Vector2(0.8f, 0.44f);
    [SerializeField] private float verticalOffset = 1f;

    private const string PickupChildName = "JumpSkillPickupOrb";
    private const string FrameResourcePrefix = "Pickups/WingFlap_";
    private const int FrameCount = 4;
    private const float FrameDuration = 0.14f;

    private Transform pickupOrb;
    private SpriteRenderer pickupRenderer;
    private Sprite[] pickupFrames;
    private float animationTime;
    private bool collected;

    private void OnEnable()
    {
        BuildPickup();
    }

    private void Update()
    {
        if (!Application.isPlaying || pickupOrb == null || pickupRenderer == null ||
            pickupFrames == null)
        {
            return;
        }

        animationTime = (animationTime + Time.deltaTime) % (FrameDuration * FrameCount);
        int frame = Mathf.FloorToInt(animationTime / FrameDuration);
        pickupRenderer.sprite = pickupFrames[frame];

        float flutter = Time.time * 3f;
        pickupOrb.localPosition = new Vector3(0f, verticalOffset + Mathf.Sin(flutter) * 0.025f, 0f);
        pickupOrb.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(flutter) * 2f);
    }

    private void OnDisable()
    {
        DestroyPickup();
    }

    public void Collect(PlayerController player)
    {
        if (collected || player == null) return;
        collected = true;
        player.UnlockFlight();
        DestroyPickup();
    }

    private void BuildPickup()
    {
        if (pickupOrb != null) return;

        GameObject orb = new GameObject(PickupChildName);
        orb.transform.SetParent(transform, false);
        orb.transform.localPosition = new Vector3(0f, verticalOffset, 0f);
        if (!Application.isPlaying) orb.hideFlags = HideFlags.HideAndDontSave;

        pickupFrames = LoadFrames();
        pickupRenderer = orb.AddComponent<SpriteRenderer>();
        pickupRenderer.sprite = pickupFrames != null
            ? pickupFrames[0]
            : Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        pickupRenderer.color = pickupFrames != null ? Color.white : pickupColor;
        pickupRenderer.drawMode = SpriteDrawMode.Simple;
        pickupRenderer.sortingOrder = 5;

        Vector2 spriteSize = pickupRenderer.sprite.bounds.size;
        orb.transform.localScale = new Vector3(
            pickupSize.x / Mathf.Max(0.001f, spriteSize.x),
            pickupSize.y / Mathf.Max(0.001f, spriteSize.y),
            1f);

        BoxCollider2D trigger = orb.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = spriteSize * 0.92f;
        orb.AddComponent<JumpPickupTrigger>().Configure(this);
        pickupOrb = orb.transform;
    }

    private static Sprite[] LoadFrames()
    {
        Sprite[] frames = new Sprite[FrameCount];
        for (int i = 0; i < FrameCount; i++)
        {
            frames[i] = Resources.Load<Sprite>(FrameResourcePrefix + i.ToString("00"));
            if (frames[i] == null) return null;
        }
        return frames;
    }

    private void DestroyPickup()
    {
        if (pickupOrb == null) return;
        if (Application.isPlaying) Destroy(pickupOrb.gameObject);
        else DestroyImmediate(pickupOrb.gameObject);
        pickupOrb = null;
        pickupRenderer = null;
        pickupFrames = null;
    }
}
