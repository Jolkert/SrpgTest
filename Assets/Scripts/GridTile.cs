using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TerrainUtils;

#nullable enable
public class GridTile : MonoBehaviour
{
	public Vector2Int Coordinates { get; private set; }
	public Color Color { get; private set; }
	public int MovementCost { get; private set; } = 1;

	public static readonly Color MovementHighlight = new Color(0, 0, 1, .5f);

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

	public void ResetColor() => gameObject.GetComponent<SpriteRenderer>().color = Color;
	public void SetColor(Color color) => gameObject.GetComponent<SpriteRenderer>().color = color;

	private void OnMouseDown()
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
