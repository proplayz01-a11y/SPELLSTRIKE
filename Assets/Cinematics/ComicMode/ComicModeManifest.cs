using System;
using UnityEngine;

namespace Spellstrike.ComicMode
{
    [Serializable]
    public sealed class ComicModePanel
    {
        public int id;
        public string title;
        public string imageResource;
        public string speaker;
        public string caption;
        public string textStatus;
        public string artStatus;
        public string reason;
        public string sourcePath;
        public string sourceSha256;
        public string imageSha256;
        public int imageWidth;
        public int imageHeight;
    }

    [Serializable]
    public sealed class ComicModeManifest
    {
        public int version;
        public ComicModePanel[] panels;

        public static ComicModeManifest Load()
        {
            TextAsset source = Resources.Load<TextAsset>("ComicMode/manifest");
            if (source == null)
            {
                Debug.LogError("[ComicMode] Missing Resources/ComicMode/manifest.json.");
                return null;
            }

            try
            {
                ComicModeManifest manifest = JsonUtility.FromJson<ComicModeManifest>(source.text);
                if (manifest == null || manifest.panels == null || manifest.panels.Length != 42)
                {
                    Debug.LogError("[ComicMode] Manifest must define 42 panels in order.");
                    return null;
                }

                for (int index = 0; index < manifest.panels.Length; index++)
                {
                    if (manifest.panels[index] == null ||
                        manifest.panels[index].id != index + 1 ||
                        string.IsNullOrEmpty(manifest.panels[index].imageResource))
                    {
                        Debug.LogError("[ComicMode] Invalid panel at index " + index);
                        return null;
                    }
                }

                return manifest;
            }
            catch (Exception error)
            {
                Debug.LogError("[ComicMode] Cannot parse manifest: " + error.Message);
                return null;
            }
            finally
            {
                Resources.UnloadAsset(source);
            }
        }
    }
}
