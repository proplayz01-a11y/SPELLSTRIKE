using UnityEngine;

public static class PassiveCrowdControlModifier
{
    private const float TidecarvedShellMultiplier = 0.75f;

    public static float ModifyKnockback(float force)
    {
        float sanitized = Mathf.Max(0f, force);
        if (!IsTidecarvedShellEquipped())
            return sanitized;

        float reduced = sanitized * TidecarvedShellMultiplier;
        Debug.Log($"[TidecarvedShell] Steady Current reduced knockback {sanitized:0.##} -> {reduced:0.##}.");
        return reduced;
    }

    public static float ModifyStun(float duration)
    {
        float sanitized = Mathf.Max(0f, duration);
        if (!IsTidecarvedShellEquipped())
            return sanitized;

        float reduced = sanitized * TidecarvedShellMultiplier;
        Debug.Log($"[TidecarvedShell] Steady Current reduced stun {sanitized:0.##}s -> {reduced:0.##}s.");
        return reduced;
    }

    private static bool IsTidecarvedShellEquipped()
    {
        PassiveItemInventory inventory = PassiveItemInventory.Instance;
        if (inventory == null)
            inventory = PassiveItemInventory.GetOrCreate();

        return inventory != null && inventory.IsEquipped(PassiveItemId.TidecarvedShell);
    }
}
