using UnityEngine;
using UnityEngine.UI;

public class WordBarLayoutManager : MonoBehaviour
{
    public GridLayoutGroup wordBarGrid;
    public int maxColumns = 8; // max tiles before shrinking

    public void UpdateGrid(int tileCount)
    {
        float panelWidth = ((RectTransform)wordBarGrid.transform).rect.width;
        int columns = Mathf.Min(tileCount, maxColumns);
        float spacing = wordBarGrid.spacing.x;
        float cellSize = (panelWidth - spacing * (columns - 1)) / columns;

        wordBarGrid.cellSize = new Vector2(cellSize, cellSize);
        wordBarGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        wordBarGrid.constraintCount = columns;
    }
}