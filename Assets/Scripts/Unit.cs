using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using Assets.Scripts;
using Assets.Scripts.Data;
using Assets.Scripts.Extensions;

#nullable enable
public class Unit : MonoBehaviour
{
	// Static
	public static readonly Color InactiveColor = new Color(.3f, .3f, .3f);
	
	// Instance
	private bool _isStarted = false;

	[SerializeField] private Vector2Int _pos;
	public Vector2Int Pos => _pos;

	[SerializeField] private Side _side;
	public Side Side => _side;

	[SerializeField] private Stats _stats;
	public Stats Stats => _stats;

	public int AttackSpeed => _stats.Spd;
	public int Hit => _stats.Dex * 2 + _stats.Lck/2;
	public int Avo => AttackSpeed * 2 + _stats.Lck/2;
	public int Crit => _stats.Dex/2;
	public int Ddg => _stats.Lck;

	public int CurrentHp { get; private set; }
	public bool HasMoved { get; private set; } = false;

	private bool _selected = false;
	private GameObject? _selectedBox = null;

	private Grid _map = null!; // dont get mad at me its fine

	private IReadOnlyList<GridTile>? _validMoves = null;
	public IReadOnlyList<GridTile> ValidMoves => _validMoves ??= RecalculateMovementArea();


	private void Start()
	{// Before first frame update
		CurrentHp = _stats.MaxHp;

		_map = SceneManager.GetActiveScene().GetRootGameObjects().First(it => it.CompareTag("grid")).GetComponent<Grid>();
		MoveTo(_map.GetTile(_pos));
		_map.RegisterUnit(this);

		_isStarted = true;
	}

	private void OnMouseUpAsButton()
	{
		if (HasMoved || _map.Phase != _side)
			return;

		if (!_selected)
		{
			GameObject? selectedUnit = GameObject.FindGameObjectsWithTag("selected_unit").FirstOrDefault();
			if (selectedUnit is not null)
			{
				selectedUnit.GetComponent<Unit>().SetSelected(false);
			}
		}

		SetSelected(!_selected);
	}

	public void MoveTo(GridTile tile)
	{
		_map.GetTile(_pos).OccupiedBy = Side.None;
		tile.OccupiedBy = Side;

		Vector3 tilePos = tile.gameObject.transform.position;

		gameObject.transform.SetPositionAndRotation(new Vector3(tilePos.x, tilePos.y, gameObject.transform.position.z), Quaternion.identity);
		_pos = tile.Coordinates;
		_validMoves = null;

		if (_isStarted)
		{
			HasMoved = true;
			SetColor(InactiveColor);
			_map.OnUnitAction(this);
		}
	}

	public void SetSelected(bool selected)
	{
		_selected = selected;
		_selectedBox = _selectedBox != null ? _selectedBox : gameObject.transform.GetChild(0).gameObject;

		_selectedBox.SetActive(selected);
		if (selected)
			gameObject.tag = "selected_unit";
		else
			gameObject.tag = _side.ToUnitTag();

		if (_selected)
			ValidMoves.ForEach(tile => tile.Highlight());
		else
			_map.EnumerateTileObjects().Select(obj => obj.GetComponent<GridTile>()).ForEach(tile => tile.ResetHighlight());
	}

	public List<GridTile> RecalculateMovementArea()
	{
		Dictionary<GridTile, int> visited = new();
		RecalculateMovementArea(_map.GetTile(_pos), _stats.Mov, visited);

		return visited.Keys.ToList();
	}
	private void RecalculateMovementArea(GridTile tile, int remainingMove, Dictionary<GridTile, int> visited)
	{
		if (remainingMove < 0)
			return;

		if (tile.OccupiedBy == Side.None)
			visited[tile] = remainingMove;
		
		foreach (GridTile neighbor in GetNeighborsOf(tile))
			if (neighbor.UnitCanPassThrough(this) && (!visited.ContainsKey(neighbor) || visited[neighbor] <= remainingMove - neighbor.MovementCost))
				RecalculateMovementArea(neighbor, remainingMove - neighbor.MovementCost, visited);
	}

	public void SetColor(Color color)
	{
		gameObject.GetComponent<SpriteRenderer>().color = color;
	}

	public void ResetTurn()
	{
		HasMoved = false;
		SetColor(Color.white);
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
