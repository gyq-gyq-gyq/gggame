using UnityEngine;

public sealed class JumpPickupTrigger : MonoBehaviour
{
    private JumpSkillPickup pickup;

    public void Configure(JumpSkillPickup owner)
    {
        pickup = owner;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (pickup == null)
        {
            pickup = GetComponentInParent<JumpSkillPickup>();
        }

        bool isPlayerPart =
            other.GetComponentInParent<PlayerController>() != null ||
            other.GetComponentInParent<JellySoftBody>() != null;

        if (!isPlayerPart || pickup == null)
        {
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            player = FindObjectOfType<PlayerController>();
        }

        Debug.Log("JumpPickupTrigger: player collected flight pickup.");
        pickup.Collect(player);
    }
}
