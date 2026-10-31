using UnityEngine;
using UnityEngine.UI;

/// <summary>Persistent, serialized modal and quit actions for the menu prototype.</summary>
[RequireComponent(typeof(Button))]
public sealed class MenuV2PanelAction : MonoBehaviour
{
    [SerializeField] private GameObject panelToClose;
    [SerializeField] private bool exitGame;

    public void Configure(GameObject panel, bool quit)
    {
        panelToClose = panel;
        exitGame = quit;
    }

    private void OnEnable() => GetComponent<Button>().onClick.AddListener(Activate);
    private void OnDisable() => GetComponent<Button>().onClick.RemoveListener(Activate);

    private void Activate()
    {
        if (exitGame)
        {
            Debug.Log("[MenuV2] Quit confirmed.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        else if (panelToClose != null)
        {
            panelToClose.SetActive(false);
        }
    }
}
