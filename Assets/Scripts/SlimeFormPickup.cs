using UnityEngine;

[DisallowMultipleComponent]
public sealed class SlimeFormPickup : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float frameRate = 7f;
    [SerializeField, Min(0f)] private float bobAmplitude = 0.035f;

    private const string FrameResourcePrefix = "Pickups/SlimePickup_";
    private const int FrameCount = 4;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D trigger;
    private Sprite[] frames;
    private Vector3 basePosition;
    private bool collected;
    private float animationTime;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        trigger = GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        basePosition = transform.localPosition;
        frames = LoadFrames();
        if (frames != null)
        {
            spriteRenderer.sprite = frames[0];
            spriteRenderer.color = Color.white;
        }
    }

    private void Update()
    {
        if (frames == null || spriteRenderer == null || !Application.isPlaying) return;
        animationTime += Time.deltaTime;
        spriteRenderer.sprite = frames[Mathf.FloorToInt(animationTime * frameRate) % frames.Length];
        float bob = Time.time * 3.2f;
        transform.localPosition = basePosition + new Vector3(0f, Mathf.Abs(Mathf.Sin(bob)) * bobAmplitude, 0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected ||
            (other.GetComponentInParent<PlayerController>() == null &&
             other.GetComponentInParent<JellySoftBody>() == null)) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) player = FindObjectOfType<PlayerController>();
        if (player == null) return;

        collected = true;
        player.UnlockSlimeForm();
        gameObject.SetActive(false);
    }

    private static Sprite[] LoadFrames()
    {
        Sprite[] loaded = new Sprite[FrameCount];
        for (int i = 0; i < FrameCount; i++)
        {
            loaded[i] = Resources.Load<Sprite>(FrameResourcePrefix + i.ToString("00"));
            if (loaded[i] == null) return null;
        }
        return loaded;
    }
}
