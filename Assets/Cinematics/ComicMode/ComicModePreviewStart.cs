using UnityEngine;

namespace Spellstrike.ComicMode
{
    // Isolated preview scene convenience component; run only in Play Mode.
    // The actual 3D cinematic option is supplied by the master sequence.
    public sealed class ComicModePreviewStart : MonoBehaviour
    {
        private void Start()
        {
            ComicModeSelection.Open(null, null, () => ComicModeSelection.Open(null));
        }
    }
}
