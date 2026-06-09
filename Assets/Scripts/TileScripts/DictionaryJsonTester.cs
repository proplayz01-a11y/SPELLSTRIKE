using UnityEngine;

public class DictionaryJsonTester : MonoBehaviour
{
    void Start()
    {
        TextAsset jsonFile =
            Resources.Load<TextAsset>("clean_gameplay_dictionary");

        if (jsonFile == null)
        {
            Debug.LogError("JSON NOT FOUND");
            return;
        }

        GameplayDictionaryWrapper wrapper =
            JsonUtility.FromJson<GameplayDictionaryWrapper>(
                jsonFile.text
            );

        Debug.Log("Loaded Entries: " + wrapper.entries.Length);

        if(wrapper.entries.Length > 0)
        {
            Debug.Log(
                wrapper.entries[0].word +
                " | " +
                wrapper.entries[0].definition
            );
        }
    }
}