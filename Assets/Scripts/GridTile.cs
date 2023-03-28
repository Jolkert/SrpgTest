using Assets.Scripts.Data;
using Assets.Scripts.Structure;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

#nullable enable
public class GridTile : MonoBehaviour
{
	// static
	public static readonly Color MovementHighlight = new Color(0, 0, 1, .5f);
	public static readonly Color AttackHighlight = new Color(1, 0, 0, .5f);

	public static readonly Vector2Int[] NeighborOffsets = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

	// instance
	public Vector2Int Coordinates { get; private set; }
	public int MovementCost { get; private set; } = 1;
	public Side OccupiedBy { get; set; } = Side.None;

	public Grid ParentGrid => GetComponentInParent<Grid>();

	private GridTile() { }
	public static GridTile AddTo(GameObject obj, Vector2Int coordinates)
	{
		GridTile self = obj.AddComponent<GridTile>();
		self.Coordinates = coordinates;

		self.MovementCost = Random.Range(0, 10) == 0 ? 2 : 1;
		if (self.MovementCost > 1)
		{
			obj.GetComponent<SpriteRenderer>().sprite = obj.GetComponentInParent<Grid>().ForestSprite;
		}

		return self;
	}

	private void OnMouseUpAsButton()
	{
		GameObject? selectedUnitObj = SceneManager.GetActiveScene().GetRootGameObjects().SingleOrDefault(it => it.CompareTag("selected_unit"));
		if (selectedUnitObj == null)
			return;

		Unit selectedUnit = selectedUnitObj.GetComponent<Unit>();
		if (selectedUnit.AwaitingAction)
			return;

		if (selectedUnit.ValidMoves.ContainsKey(this) && selectedUnit.ValidMoves[this] == RangeType.Movement)
			selectedUnit.MoveTo(this);
	}

	public void Highlight() => Highlight(MovementHighlight);
	public void Highlight(RangeType type) => Highlight(type switch
	{
		RangeType.Movement => MovementHighlight,
		RangeType.Attack => AttackHighlight,
		_ => MovementHighlight
	});
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

	public IEnumerable<GridTile> GetNeighbors()
	{
		foreach (Vector2Int offset in NeighborOffsets)
			if (ParentGrid.TryGetTile(Coordinates + offset, out GridTile? tile))
				yield return tile;
	}
	public IEnumerable<GridTile> GetTilesWithinRange(Weapon.WeaponRange range)
	{
		if (range == 1)
		{
			foreach (GridTile neighbor in GetNeighbors())
				yield return neighbor;

			yield break;
		}

		UniqueQueue<(GridTile tile, int depth)> processingQueue = new();
		List<GridTile> visited = new();

		processingQueue.Enqueue((this, 0));
		while (processingQueue.Count > 0)
		{
			(GridTile tile, int depth) = processingQueue.Dequeue();

			if (range.Contains(depth))
				yield return tile;

			if (depth < range.Max)
			{
				if (!visited.Contains(tile))
					visited.Add(tile);

				foreach (GridTile neighbor in tile.GetNeighbors())
					if (!visited.Contains(neighbor))
						processingQueue.Enqueue((neighbor, depth + 1));
			}
		}
	}

	public override string ToString() => $"{Coordinates} | {MovementCost}mov | {OccupiedBy}";
}
