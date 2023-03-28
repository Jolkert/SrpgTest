using Assets.Scripts;
using Assets.Scripts.Data;
using Assets.Scripts.Extensions;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

#nullable enable
public class Unit : MonoBehaviour
{
	// static
	public static readonly Color InactiveColor = new Color(.3f, .3f, .3f);

	// instance
	private bool _isStarted = false; // compiler claims that the value is never used but it literally is in MoveTo()??? u good compiler? -morgan 2023-03-28
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

	private Vector2Int _awaitingPos;
	private Vector3 _prevObjectPos;
	public bool AwaitingAction { get; private set; } = false;
	public List<GridTile>? AwaitingAttackRange { get; private set; } = null;

	private void Start()
	{// Before first frame update
		CurrentHp = _stats.MaxHp;

		_map = SceneManager.GetActiveScene().GetRootGameObjects().First(it => it.CompareTag("grid")).GetComponent<Grid>();
		MoveTo(_map[_pos]);

		_isStarted = true;
	}
	private void Update()
	{
		if (_selected && AwaitingAction)
			ProcessKeyInput();
	}
	private void OnMouseUpAsButton()
	{
		// unity's fake null makes me so fucking sad. please just let me use null propagation -morgan 2023-03-28
		GameObject? temp = GameObject.FindGameObjectsWithTag("selected_unit").FirstOrDefault();
		Unit? selectedUnit = temp != null ? temp.GetComponent<Unit>() : null;

		if (selectedUnit == null)
		{
			if (!HasMoved && _map.Phase == Side)
				SetSelected(true);
		}
		else
		{
			if (selectedUnit == this)
			{
				SetSelected(false);
				return;
			}

			if (selectedUnit.AwaitingAction && selectedUnit.AwaitingAttackRange.Select(tile => tile.Coordinates).Contains(_pos))
				selectedUnit.AttackAction(this);
		}

		//if (HasMoved || _map.Phase != _side)
		//	return;
		//
		//if (!_selected)
		//{
		//	GameObject? selectedUnit = GameObject.FindGameObjectsWithTag("selected_unit").FirstOrDefault();
		//	if (selectedUnit is not null)
		//	{
		//		selectedUnit.GetComponent<Unit>().SetSelected(false);
		//	}
		//}
		//
		//SetSelected(!_selected);
	}
	private void OnMouseOver()
	{
		_map.SetDebugText(CreateDebugString());
	}

	private void ProcessKeyInput()
	{
		if (Input.GetKeyUp(KeyCode.Escape))
			UndoMovement();
		else if (Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.KeypadEnter))
			WaitAction();
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
			ValidMoves.ForEach(entry => entry.Key.Highlight(entry.Value));
		else
			_map.ForEach(tile => tile.ResetHighlight());
	}
	public void MoveTo(GridTile tile)
	{
		_prevObjectPos = transform.position;

		Vector3 tilePos = tile.gameObject.transform.position;
		gameObject.transform.SetPositionAndRotation(new Vector3(tilePos.x, tilePos.y, transform.position.z), Quaternion.identity);
		if (!_isStarted)
			return;
		
		AwaitingAction = true;
		_awaitingPos = tile.Coordinates;

		_map.ForEach(t => t.ResetHighlight());

		AwaitingAttackRange = tile.GetTilesWithinRange(Weapon.Range).ToList();
		AwaitingAttackRange.ForEach(t => t.Highlight(RangeType.Attack));
	}
	public void UndoMovement()
	{
		transform.SetPositionAndRotation(_prevObjectPos, Quaternion.identity);
		SetSelected(false);
		AwaitingAction = false;
		_map.ForEach(t => t.ResetHighlight());
	}

	public void Action()
	{
		HasMoved = true;
		SetColor(InactiveColor);
		_map.OnUnitAction(this);

		AwaitingAction = false;
		SetSelected(false);
	}
	public void AttackAction(Unit target)
	{
		int dmg = System.Math.Max(0, Atk - (Weapon.IsMagic ? target.Stats.Res : target.Stats.Def));
		target.CurrentHp = System.Math.Max(0, target.CurrentHp - dmg);

		Debug.Log($"{gameObject.name} dealt {dmg} damage to {target.gameObject.name}");

		Action();
	}
	public void WaitAction()
	{
		_pos = _awaitingPos;
		Action();
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

		RecalculateMovementArea(_map[_pos], _stats.Mov, visited, attackRange);

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
