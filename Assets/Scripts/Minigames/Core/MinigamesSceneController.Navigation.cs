using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

// Maintained as a partial MonoBehaviour to preserve all existing Unity scene references.
public partial class MinigamesSceneController
{
    private void ShowInitialScreen()
    {
        switch (startupScene)
        {
            case MinigameSceneStartup.ModeSelect:
                ShowModeSelection();
                ApplySceneIsolation();
                return;
            case MinigameSceneStartup.WordMaster:
                SelectMode(MinigameMode.WordMaster);
                ApplySceneIsolation();
                return;
            case MinigameSceneStartup.SpellItOut:
                SelectMode(MinigameMode.SpellItOut);
                ApplySceneIsolation();
                return;
            case MinigameSceneStartup.WordExcavation:
                SelectMode(MinigameMode.WordExcavation);
                ApplySceneIsolation();
                return;
        }

        bool startInWordMaster = useEditableSceneUI
            && playScreen != null
            && playScreen.gameObject.activeSelf
            && (selectionScreen == null || !selectionScreen.gameObject.activeSelf);

        if (startInWordMaster)
        {
            Debug.Log("[WordMaster] Play screen active at start. Starting Word Master directly.");
            SelectMode(MinigameMode.WordMaster);
            return;
        }

        Debug.Log("[WordMaster] Showing minigame mode selection.");
        ShowModeSelection();
    }

    private void Update()
    {
        if (minigamePaused || resultOverlayShowing)
            return;

        if (excavationActive)
        {
            if (excavationSession.Tick(Time.deltaTime))
            {
                UpdateTimerText();
                ShowMinigameResultOverlay("Time Up");
                return;
            }

            UpdateTimerText();
        }

        if (spellItOutActive)
        {
            if (spellingSession.Tick(Time.deltaTime))
            {
                UpdateTimerText();
                HandleSpellItOutWordTimeout();
                return;
            }

            UpdateTimerText();
        }
    }

    public void SelectMode(MinigameMode mode)
    {
        currentMode = mode;
        score = 0;
        roundsPlayed = 0;
        excavationActive = false;
        spellItOutActive = false;
        HideMinigameOverlays();
        foundExcavationWords.Clear();
        foundExcavationWordsInOrder.Clear();

        if (selectionScreen != null)
            selectionScreen.gameObject.SetActive(false);

        if (playScreen != null)
            playScreen.gameObject.SetActive(true);

        StartNewRound();
    }

    public void ShowModeSelection()
    {
        excavationActive = false;
        spellItOutActive = false;
        HideMinigameOverlays();
        ClearOptionButtons();

        if (selectionScreen != null)
            selectionScreen.gameObject.SetActive(true);

        if (playScreen != null)
            playScreen.gameObject.SetActive(false);
    }

    public void OpenWordMasterScene()
    {
        OpenConfiguredMinigameScene(wordMasterSceneName, MinigameMode.WordMaster);
    }

    public void OpenSpellItOutScene()
    {
        OpenConfiguredMinigameScene(spellItOutSceneName, MinigameMode.SpellItOut);
    }

    public void OpenWordExcavationScene()
    {
        OpenConfiguredMinigameScene(wordExcavationSceneName, MinigameMode.WordExcavation);
    }

    public void ReturnToModeSelect()
    {
        if (!string.IsNullOrWhiteSpace(modeSelectSceneName))
        {
            SceneManager.LoadScene(modeSelectSceneName);
            return;
        }

        ShowModeSelection();
    }

    private void OpenConfiguredMinigameScene(string sceneName, MinigameMode fallbackMode)
    {
        if (!string.IsNullOrWhiteSpace(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        SelectMode(fallbackMode);
    }

    public void ApplySceneIsolation()
    {
        if (startupScene == MinigameSceneStartup.ModeSelect)
        {
            if (playScreen != null)
            {
                DestroySafe(playScreen.gameObject);
                playScreen = null;
            }

            return;
        }

        if (startupScene == MinigameSceneStartup.WordMaster
            || startupScene == MinigameSceneStartup.SpellItOut
            || startupScene == MinigameSceneStartup.WordExcavation)
        {
            if (selectionScreen != null)
            {
                DestroySafe(selectionScreen.gameObject);
                selectionScreen = null;
            }
        }
    }

    public void StartNewRound()
    {
        HideMinigameOverlays();
        ClearOptionButtons();
        SetFeedback(string.Empty, lightTextColor);

        switch (currentMode)
        {
            case MinigameMode.WordMaster:
                StartWordMasterRound();
                break;
            case MinigameMode.SpellItOut:
                StartSpellItOutRound();
                break;
            case MinigameMode.WordExcavation:
                StartExcavationRound();
                break;
        }

        RefreshScoreText();
    }

    public void SubmitTypedAnswer()
    {
        if (currentMode == MinigameMode.WordMaster)
        {
            string tileAnswer = GetWordMasterCurrentGuess();
            if (string.IsNullOrWhiteSpace(tileAnswer))
                return;

            ResolveWordMasterGuess(tileAnswer);
            return;
        }

        if (currentMode == MinigameMode.SpellItOut)
        {
            string tileAnswer = GetSelectedTileWord();
            if (string.IsNullOrWhiteSpace(tileAnswer))
                return;

            ResolveSpellItOut(tileAnswer);
            return;
        }

        if (currentMode == MinigameMode.WordExcavation)
        {
            string tileAnswer = GetSelectedTileWord();
            if (string.IsNullOrWhiteSpace(tileAnswer))
                return;

            ResolveExcavationAnswer(tileAnswer);
            return;
        }

        if (answerInput == null)
            return;

        string answer = answerInput.text.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(answer))
            return;
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
