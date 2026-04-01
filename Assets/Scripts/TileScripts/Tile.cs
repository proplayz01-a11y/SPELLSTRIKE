using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Tile : MonoBehaviour
{
    public char letter;
    public Button button;              // button on the UI prefab
    public TextMeshProUGUI textTMP;    // TMP text
    public Text letterText;            // fallback Text

    private TileManager tileManager;

    void Start()
    {
        tileManager = Object.FindAnyObjectByType<TileManager>();
        if (button != null)
            button.onClick.AddListener(OnClick);

        UpdateVisual();
    }

    // call this every time the letter changes
    public void SetLetter(char newLetter)
    {
        letter = newLetter;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (textTMP != null)
            textTMP.text = letter.ToString();
        if (letterText != null)
            letterText.text = letter.ToString();
    }

    private void OnClick()
    {
        if (tileManager != null)
            tileManager.ToggleTilePanel(this);
    }

   
}