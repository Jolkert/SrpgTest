using Assets.Scripts;
using Assets.Scripts.Data;
using Assets.Scripts.Extensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

#nullable enable
public class Unit : MonoBehaviour
{
	// static
	public static readonly Color InactiveColor = new Color(.3f, .3f, .3f);

	// instance
	private bool _isStarted = false;
	private Grid _map = null!;

	[SerializeField] private Vector2Int _pos;
	public Vector2Int Pos => _pos;

	[SerializeField] private Side _side;
	public Side Side => _side;

	[SerializeField] private Stats _stats;
	public Stats Stats => _stats;

	[SerializeField] private Weapon _weapon;
	public Weapon Weapon => _weapon;

	public int Atk => _weapon.Mt + (_weapon.IsMagic ? _stats.Mag : _stats.Str);
	public int AttackSpeed => _stats.Spd - System.Math.Max(_weapon.Wt - _stats.Con, 0);

	public int Hit => _weapon.Hit + _stats.Dex * 2 + _stats.Lck / 2;
	public int Avo => AttackSpeed * 2 + _stats.Lck / 2;
	public int Crit => Weapon.Crit + _stats.Dex / 2;
	public int Ddg => _stats.Lck;

	public int CurrentHp { get; private set; }
	public bool HasMoved { get; private set; } = false;

	private IReadOnlyDictionary<GridTile, RangeType>? _validMoves = null;
	public IReadOnlyDictionary<GridTile, RangeType> ValidMoves => _validMoves ??= RecalculateMovementArea();

	private bool _selected = false;
	private GameObject? _selectedBox = null;

	private void Start()
	{// Before first frame update
		CurrentHp = _stats.MaxHp;

		_map = SceneManager.GetActiveScene().GetRootGameObjects().First(it => it.CompareTag("grid")).GetComponent<Grid>();
		MoveTo(_map.GetTile(_pos));

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
		{
			ValidMoves.ForEach(entry => entry.Key.Highlight(entry.Value));
			_map.SetDebugText(CreateDebugString());
		}
		else
		{
			_map.ForEach(tile => tile.ResetHighlight());
			_map.SetDebugText("");
		}
	}
	public void MoveTo(GridTile tile)
	{
		_map.GetTile(_pos).OccupiedBy = Side.None;
		tile.OccupiedBy = Side;

		Vector3 tilePos = tile.gameObject.transform.position;

		gameObject.transform.SetPositionAndRotation(new Vector3(tilePos.x, tilePos.y, gameObject.transform.position.z), Quaternion.identity);
		_pos = tile.Coordinates;

		if (_isStarted)
		{
			HasMoved = true;
			SetColor(InactiveColor);
			_map.OnUnitMove(this);
		}

		_validMoves = null;
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

	public IReadOnlyDictionary<GridTile, RangeType> RecalculateMovementArea()
	{
		Dictionary<GridTile, int> visited = new();
		HashSet<GridTile> attackRange = new();

		RecalculateMovementArea(_map.GetTile(_pos), _stats.Mov, visited, attackRange);

		Dictionary<GridTile, RangeType> movementArea = new();
		foreach (GridTile tile in attackRange)
			if (tile.OccupiedBy != Side)
				movementArea.Add(tile, RangeType.Attack);

		foreach (KeyValuePair<GridTile, int> entry in visited)
			movementArea[entry.Key] = RangeType.Movement;

		return movementArea;
	}
	private void RecalculateMovementArea(GridTile tile, int remainingMove, Dictionary<GridTile, int> visited, HashSet<GridTile> attackRange)
	{
		if (remainingMove < 0)
			return;

		if (tile.OccupiedBy == Side.None)
		{
			visited[tile] = remainingMove;
			foreach (GridTile square in tile.GetTilesWithinRange(Weapon.Range))
				attackRange.Add(square);
		}

		foreach (GridTile neighbor in tile.GetNeighbors())
			if (neighbor.UnitCanPassThrough(this) && (!visited.ContainsKey(neighbor) || visited[neighbor] <= remainingMove - neighbor.MovementCost))
				RecalculateMovementArea(neighbor, remainingMove - neighbor.MovementCost, visited, attackRange);
	}

	public void ResetValidMoves() => _validMoves = null;

	private string CreateDebugString()
	{
		return $"HP: {CurrentHp}/{_stats.MaxHp}\n" +
			   $"{_stats.PrettyString()}\n\n" +
			   $"Atk: {Atk}\n" +
			   $"ASpd: {AttackSpeed}\n" +
			   $"Hit: {Hit}\n" +
			   $"Avo: {Avo}\n" +
			   $"Crit: {Crit}\n" +
			   $"Ddg: {Ddg}\n";
	}
}
