using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StorySequenceProfile", menuName = "SPELLSTRIKE/Story/Sequence Profile")]
public class StorySequenceProfile : ScriptableObject
{
    [Serializable]
    public class Page
    {
        [SerializeField] private string pageId;
        [SerializeField] private Sprite pageSprite;
        [SerializeField] private AudioClip optionalAudio;
        [SerializeField, Min(0f)] private float minimumDisplayTime;
        [SerializeField] private string optionalNextSceneOverride;
        [SerializeField, TextArea(2, 5)] private string fallbackText;

        public string PageId => pageId;
        public Sprite PageSprite => pageSprite;
        public AudioClip OptionalAudio => optionalAudio;
        public float MinimumDisplayTime => minimumDisplayTime;
        public string OptionalNextSceneOverride => optionalNextSceneOverride;
        public string FallbackText => fallbackText;
    }

    [SerializeField] private string sequenceId = "prologue";
    [SerializeField] private string completionKey = "SpellStrike_PrologueCompleted";
    [SerializeField] private string defaultNextSceneName = "TutorialScene";
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private List<Page> pages = new List<Page>();

    public string SequenceId => sequenceId;
    public string CompletionKey => completionKey;
    public bool AllowSkip => allowSkip;
    public IReadOnlyList<Page> Pages => pages;

    public string ResolveNextScene(Page currentPage)
    {
        if (currentPage != null && !string.IsNullOrWhiteSpace(currentPage.OptionalNextSceneOverride))
            return currentPage.OptionalNextSceneOverride;
        return defaultNextSceneName;
    }
}
