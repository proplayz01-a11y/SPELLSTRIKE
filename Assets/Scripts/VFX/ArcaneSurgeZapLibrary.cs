using UnityEngine;

namespace Spellstrike.Vfx
{
    /// <summary>
    /// Runtime addressable references to the vendor's imported prefabs.
    /// Keeps the vendor assets untouched and avoids copying 1 MB prefabs
    /// into Resources. All refs live in ArcaneSurgeZapLibrary.asset.
    /// </summary>
    public sealed class ArcaneSurgeZapLibrary : ScriptableObject
    {
        public const string ResourcePath = "Spellstrike/VFX/ArcaneSurgeZapLibrary";

        [SerializeField] private GameObject yellowZap;
        [SerializeField] private GameObject darkBlueZap;
        [SerializeField] private GameObject cyanBlueZap;
        [SerializeField] private AudioClip bigZapSound;

        public GameObject Yellow => yellowZap;
        public GameObject DarkBlue => darkBlueZap;
        public GameObject CyanBlue => cyanBlueZap;
        public AudioClip BigZapSound => bigZapSound;

        public bool IsConfigured =>
            yellowZap != null && darkBlueZap != null && cyanBlueZap != null;
    }
}
