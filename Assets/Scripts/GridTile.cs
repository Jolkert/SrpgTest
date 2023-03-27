using Assets.Scripts.Data;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

#nullable enable
public class GridTile : MonoBehaviour
{
	public Vector2Int Coordinates { get; private set; }
	public Color Color { get; private set; }
	public int MovementCost { get; private set; } = 1;

	public static readonly Color MovementHighlight = new Color(0, 0, 1, .5f);

	public Side OccupiedBy = Side.None;

	private GridTile() { }

	public static GridTile AddTo(GameObject obj, Vector2Int coordinates)
	{
		GridTile self = obj.AddComponent<GridTile>();
		self.Coordinates = coordinates;
		self.Color = obj.GetComponent<SpriteRenderer>().color;

		self.MovementCost = Random.Range(0, 10) == 0 ? 2 : 1;
		if (self.MovementCost > 1)
		{
			obj.GetComponent<SpriteRenderer>().sprite = obj.GetComponentInParent<Grid>().ForestSprite;
		}

		return self;
	}

	public void Highlight() => Highlight(MovementHighlight);
	public void Highlight(Color color)
	{
		GameObject selectionBox = Instantiate(GetComponentInParent<Grid>().HighlightBox, Vector3.zero, Quaternion.identity);
		selectionBox.transform.SetParent(gameObject.transform, false);
		selectionBox.GetComponent<SpriteRenderer>().color = color;
	}
	public void ResetHighlight()
	{
		foreach (Transform child in transform)
			Destroy(child.gameObject);
	}

	public bool UnitCanPassThrough(Unit unit) => OccupiedBy == Side.None || OccupiedBy == unit.Side;

	private void OnMouseUpAsButton()
	{
		GameObject? selectedUnitObj = SceneManager.GetActiveScene().GetRootGameObjects().SingleOrDefault(it => it.CompareTag("selected_unit"));
		if (selectedUnitObj is null)
			return;

		Unit selectedUnit = selectedUnitObj.GetComponent<Unit>();
		if (selectedUnit.ValidMoves.Contains(this))
			selectedUnit.MoveTo(this);

		selectedUnit.SetSelected(false);
	}
}
