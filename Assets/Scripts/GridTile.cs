using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

#nullable enable
public class GridTile : MonoBehaviour
{
	public Vector2Int Coordinates { get; private set; }
	public Color Color { get; private set; }
	public int MovementCost { get; private set; } = 1;

	private static Color Black2Mov = new Color(0, .5f, 0);
	private static Color White2Mov = new Color(.5f, 1, .5f);

	private GridTile() { }

	public static GridTile AddTo(GameObject obj, Vector2Int coordinates)
	{
		GridTile self = obj.AddComponent<GridTile>();
		self.Coordinates = coordinates;
		self.Color = obj.GetComponent<SpriteRenderer>().color;

		self.MovementCost = Random.Range(0, 10) == 0 ? 2 : 1;
		if (self.MovementCost > 1)
		{
			if (self.Color == Color.black)
				self.Color = Black2Mov;
			else
				self.Color = White2Mov;

			obj.GetComponent<SpriteRenderer>().color = self.Color;
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
