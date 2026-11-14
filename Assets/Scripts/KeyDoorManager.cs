using UnityEngine;

/// <summary>
/// Tracks all keys in the current relay level. Keys may be collected in any order.
/// </summary>
public class KeyDoorManager : MonoBehaviour
{
    public static KeyDoorManager Instance { get; private set; }

    public int RequiredKeys { get; private set; }
    public int CollectedKeys { get; private set; }
    public bool HasAllKeys => RequiredKeys > 0 && CollectedKeys >= RequiredKeys;
    public bool IsComplete { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterKey()
    {
        RequiredKeys++;
    }

    public void CollectKey()
    {
        CollectedKeys = Mathf.Min(CollectedKeys + 1, RequiredKeys);
    }

    public void CompleteLevel()
    {
        if (!HasAllKeys || IsComplete)
        {
            return;
        }

        IsComplete = true;
        Debug.Log("Level complete: all keys collected and exit reached.");
    }
}
