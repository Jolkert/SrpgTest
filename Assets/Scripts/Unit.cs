using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using JetBrains.Annotations;
using UnityEngine.Tilemaps;
using UnityEngine.XR;

#nullable enable
public class Unit : MonoBehaviour
{
	[SerializeField] private Vector2Int _pos;
	public Vector2Int Pos { get => _pos; private set => _pos = value; }

	[SerializeField] private int _movement;
	private bool _selected = false;
	private GameObject? _selectedBox = null;

	private Grid _map = null!; // dont get mad at me its fine

	private IReadOnlyList<GridTile>? _validMoves = null;
	public IReadOnlyList<GridTile> ValidMoves
	{
		get
		{
			if (_validMoves == null)
				RecalculateMovementArea();

			return _validMoves!;
		}
	}


	private void Start()
	{// Before first frame update
		_map = SceneManager.GetActiveScene().GetRootGameObjects().First(it => it.CompareTag("grid")).GetComponent<Grid>();
		MoveTo(_map.GetTile(_pos));
	}

	private void Update()
	{// Once per frame

	}

	private void OnMouseDown()
	{
		SetSelected(!_selected);
	}

	public void MoveTo(GridTile tile)
	{
		Vector3 tilePos = tile.gameObject.transform.position;

		gameObject.transform.SetPositionAndRotation(new Vector3(tilePos.x, tilePos.y, gameObject.transform.position.z), Quaternion.identity);
		Pos = tile.Coordinates;
		_validMoves = null;
	}

	public void SetSelected(bool selected)
	{
		_selected = selected;
		_selectedBox = _selectedBox != null ? _selectedBox : gameObject.transform.GetChild(0).gameObject;

		_selectedBox.SetActive(selected);
		if (selected)
			gameObject.tag = "selected_unit";
		else
			gameObject.tag = "unit";

		if (_selected)
		{
			foreach (GridTile tile in ValidMoves)
				tile.SetColor(Color.blue);
		}
		else
		{
			foreach (GridTile tile in _map.EnumerateTiles().Select(it => it.GetComponent<GridTile>()))
				tile.ResetColor();
		}
	}

	public void RecalculateMovementArea()
	{
		Dictionary<GridTile, int> visited = new();
		RecalculateMovementArea(_map.GetTile(_pos), _movement, visited);

		_validMoves = visited.Keys.ToList();
	}
	private void RecalculateMovementArea(GridTile tile, int remainingMove, Dictionary<GridTile, int> visited)
	{
		if (remainingMove < 0)
			return;

		visited[tile] = remainingMove;
		foreach (GridTile neighbor in GetNeighborsOf(tile))
			if (!visited.ContainsKey(neighbor) || visited[neighbor] <= remainingMove - neighbor.MovementCost)
				RecalculateMovementArea(neighbor, remainingMove - neighbor.MovementCost, visited);
	}

	private IEnumerable<GridTile> GetNeighborsOf(GridTile tile)
	{
		int x = tile.Coordinates.x, y = tile.Coordinates.y;

		if (y < _map.Height - 1)
			yield return _map.GetTile(x, y + 1);
		if (x < _map.Width - 1)
			yield return _map.GetTile(x + 1, y);
		if (y > 0)
			yield return _map.GetTile(x, y - 1);
		if (x > 0)
			yield return _map.GetTile(x - 1, y);
	}
}
