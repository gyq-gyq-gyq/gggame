using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
public sealed class JumpSkillPickup : MonoBehaviour
{
    [SerializeField] private Color pickupColor = new Color(1f, 0.78f, 0.12f, 1f);
    [SerializeField] private Vector2 pickupSize = new Vector2(0.72f, 0.72f);
    [SerializeField] private float verticalOffset = 1f;

    private const string PickupChildName = "JumpSkillPickupOrb";

    private Transform pickupOrb;
    private bool collected;

    private void OnEnable()
    {
        BuildPickup();
    }

    private void OnDisable()
    {
        DestroyPickup();
    }

    public void Collect(PlayerController player)
    {
        if (collected || player == null)
        {
            return;
        }

        collected = true;
        player.UnlockFlight();
        DestroyPickup();
    }

    private void BuildPickup()
    {
        if (pickupOrb != null)
        {
            return;
        }

        GameObject orb = new GameObject(PickupChildName);
        orb.transform.SetParent(transform, false);
        orb.transform.localPosition = new Vector3(0f, verticalOffset, 0f);

        if (!Application.isPlaying)
        {
            orb.hideFlags = HideFlags.HideAndDontSave;
        }

        SpriteRenderer renderer = orb.AddComponent<SpriteRenderer>();
        renderer.sprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        renderer.color = pickupColor;
        renderer.drawMode = SpriteDrawMode.Simple;
        renderer.sortingOrder = 5;

        Vector2 spriteSize = renderer.sprite.bounds.size;
        orb.transform.localScale = new Vector3(
            pickupSize.x / Mathf.Max(0.001f, spriteSize.x),
            pickupSize.y / Mathf.Max(0.001f, spriteSize.y),
            1f);

        BoxCollider2D trigger = orb.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = pickupSize * 0.92f;

        JumpPickupTrigger pickupTrigger = orb.AddComponent<JumpPickupTrigger>();
        pickupTrigger.Configure(this);
        pickupOrb = orb.transform;
    }

    private void DestroyPickup()
    {
        if (pickupOrb == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(pickupOrb.gameObject);
        }
        else
        {
            DestroyImmediate(pickupOrb.gameObject);
        }

        pickupOrb = null;
    }
}
