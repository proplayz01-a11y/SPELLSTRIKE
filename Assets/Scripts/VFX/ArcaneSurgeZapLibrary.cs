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
        [Header("Strong attack AoE - imported Hovl VFX")]
        [SerializeField] private GameObject magicCircle;
        [SerializeField] private GameObject plexusAoe;
        [SerializeField] private GameObject lightningAura;

        public GameObject Yellow => yellowZap;
        public GameObject DarkBlue => darkBlueZap;
        public GameObject CyanBlue => cyanBlueZap;
        public AudioClip BigZapSound => bigZapSound;
        public GameObject MagicCircle => magicCircle;
        public GameObject PlexusAoe => plexusAoe;
        public GameObject LightningAura => lightningAura;

        public bool IsConfigured =>
            yellowZap != null && darkBlueZap != null && cyanBlueZap != null;
        public bool HasAreaEffects =>
            magicCircle != null && plexusAoe != null && lightningAura != null;
    }
}
