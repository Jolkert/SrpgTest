using Assets.Scripts;
using Assets.Scripts.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using static System.Math;
using TileData = MapTile.MapTileData;

#nullable enable
public class Unit : MonoBehaviour
{
	// static
	public static readonly Color InactiveColor = new Color(.3f, .3f, .3f);

	// instance
	private MapGrid _map = null!;

	[SerializeField] private Vector2Int _position;
	public Vector2Int Position => _position;

	[SerializeField] private Side _side;
	public Side Side => _side;

	[SerializeField] private Stats _stats;
	public Stats Stats => _stats;

	[SerializeField] private Weapon _weapon;
	public Weapon Weapon => _weapon;

	public int Atk => _weapon.Mt + (_weapon.IsMagic ? _stats.Mag : _stats.Str);
	public int AttackSpeed => _stats.Spd - Max(_weapon.Wt - _stats.Con, 0);

	public TileData _currentTile = TileData.Default;

	public int Hit => _weapon.Hit + (_stats.Dex * 2 + _stats.Lck / 2) ;
	public int Avo => (AttackSpeed * 2 + _stats.Lck / 2) + _currentTile.AvoBonus;
	public int Crit => Weapon.Crit + _stats.Dex / 2;
	public int Ddg => _stats.Lck;

	public int Prt => _stats.Def + _currentTile.DefBonus;
	public int Rsl => _stats.Res + _currentTile.ResBonus;

	public int CurrentHp { get; private set; }
	public bool HasMoved { get; private set; } = false;

	private IReadOnlyDictionary<MapTile, RangeType>? _validMoves = null;
	public IReadOnlyDictionary<MapTile, RangeType> ValidMoves => _validMoves ??= RecalculateMovementArea();

	public bool IsSelected { get; private set; } = false;

	private Vector2Int _awaitingPos;
	private Vector3 _prevObjectPos;

	public bool IsAwaitingAction { get; private set; } = false;
	public List<MapTile>? AwaitingAttackRange { get; private set; } = null;

	private void Start()
	{// Before first frame update
		CurrentHp = _stats.MaxHp;

		_map = SceneManager.GetActiveScene().GetRootGameObjects().First(it => it.CompareTag("grid")).GetComponent<MapGrid>();
		MoveTo(Position);

		_map.RegisterUnit(this);
	}

	public void SetSelected(bool selected)
	{
		IsSelected = selected;
		_map.SelectedUnit = selected ? this : null;

		if (IsSelected)
			ValidMoves.ForEach(entry => _map.HighlightTile(entry.Key.Position, entry.Value));
		else
			_map.ResetAllHighlights();
	}

	public void MoveTo(Vector2Int newLocation)
	{
		_map[Position].OccupyingUnit = null;
		_position = newLocation;

		_map[newLocation].OccupyingUnit = this;
		_currentTile = _map[newLocation].Data;
	}
	public void AwaitActionAt(MapTile tile)
	{
		_prevObjectPos = transform.position;
		transform.SetPositionAndRotation(_map.MapCellToWorld(tile.Position), Quaternion.identity);

		IsAwaitingAction = true;
		_awaitingPos = tile.Position;

		_map.ResetAllHighlights();
		AwaitingAttackRange = _map.GetTilesWithinRangeOf(tile, Weapon.Range).ToList();
		_map.HighlightTiles(AwaitingAttackRange.Select(it => it.Position), RangeType.Attack);
	}
	public void UndoMovement()
	{
		transform.SetPositionAndRotation(_prevObjectPos, Quaternion.identity);
		SetSelected(false);
		IsAwaitingAction = false;
		_map.ResetAllHighlights();
	}

	public void Action()
	{
		MoveTo(_awaitingPos);

		HasMoved = true;
		SetColor(InactiveColor);
		_map.OnUnitAction(this);

		IsAwaitingAction = false;
		SetSelected(false);
	}
	public void InitiateCombatWith(Unit target)
	{
		MoveTo(_awaitingPos); // this is technically sorta cringe cause we do it again as soon as we call Action(), but this means that we dont have to do any bs for tile avo stuff -morgan 2023-04-04
		Debug.Log($"------ START COMBAT ------");

		Attack(target);

		bool targetCanCounter = target.CanAttack(this);
		if (targetCanCounter)
			target.Attack(this);

		if (AttackSpeed - 5 >= target.AttackSpeed)
			Attack(target);

		if (targetCanCounter && target.AttackSpeed - 5 >= AttackSpeed)
			target.Attack(this);

		Debug.Log($"------ END COMBAT ------");
		Action();
	}
	public void WaitAction() => Action();

	public void Attack(Unit target)
	{
		int targetDefensiveStat = Weapon.IsMagic ? target.Rsl : target.Prt;
		int hitChance = Clamp(Hit - target.Avo, 0, 100);
		int critChance = Clamp(Crit - target.Ddg, 0, 100);

		Debug.Log($"{name} attacks {target.name}\nAtk: {Atk} | {(Weapon.IsMagic ? "Rsl" : "Prt")}: {targetDefensiveStat} | Hit: {hitChance}% | Crit: {critChance}%");

		int hitRn = SharedResources.Random.Next();
		int critRn = SharedResources.Random.Next();

		if (!(hitRn < hitChance))
			Debug.Log($"Miss!");
		else
		{
			int critMultiplier = critRn < critChance ? 3 : 1;
			int damage = Max(0, Atk - targetDefensiveStat);
			target.TakeDamage(damage);

			Debug.Log($"{(critMultiplier == 1 ? "Hit!" : "Crit!")}\n{target.name} takes {damage} damge (hp: {target.CurrentHp}/{target._stats.MaxHp})");
		}
	}
	public void TakeDamage(int damage) => CurrentHp = Clamp(CurrentHp - damage, 0, _stats.MaxHp);
	public bool CanAttack(Unit target)
	{
		Vector2Int targetPos = target.IsAwaitingAction ? target._awaitingPos : target.Position;
		return _map.GetTilesWithinRangeOf(_map[Position], Weapon.Range).Select(tile => tile.Position).Contains(targetPos);
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

	public IReadOnlyDictionary<MapTile, RangeType> RecalculateMovementArea()
	{
		Dictionary<MapTile, int> visited = new();
		HashSet<MapTile> attackRange = new();

		RecalculateMovementArea(_map[Position], Stats.Mov, visited, attackRange);

		Dictionary<MapTile, RangeType> movementArea = new Dictionary<MapTile, RangeType>();
		foreach (MapTile tile in attackRange)
			if (tile.OccupyingSide != Side)
				movementArea.Add(tile, RangeType.Attack);

		foreach (KeyValuePair<MapTile, int> entry in visited)
			movementArea[entry.Key] = RangeType.Movement;

		return movementArea;
	}
	private void RecalculateMovementArea(MapTile tile, int remainingMove, Dictionary<MapTile, int> visited, HashSet<MapTile> attackRange)
	{
		if (remainingMove < 0)
			return;

		if (tile.OccupyingSide == Side.None)
		{
			visited[tile] = remainingMove;
			foreach (MapTile square in _map.GetTilesWithinRangeOf(tile, Weapon.Range))
				attackRange.Add(square);
		}

		foreach (MapTile neighbor in _map.GetNeighborsOf(tile))
			if (neighbor.IsPassableBy(this) && (!visited.ContainsKey(neighbor) || visited[neighbor] <= remainingMove - neighbor.Data.MovementCost))
				RecalculateMovementArea(neighbor, remainingMove - neighbor.Data.MovementCost, visited, attackRange);
	}

	public void ResetValidMoves() => _validMoves = null;

	public bool IsOpponentTo(Side side) => side != Side.None && Side != side;
	public bool CanMoveTo(MapTile tile) => ValidMoves.TryGetValue(tile, out RangeType type) && type == RangeType.Movement;

	public string ToDebugString()
	{
		return $"{name}\n" +
			   $"HP: {CurrentHp}/{_stats.MaxHp}\n" +
			   $"{_stats.PrettyString()}\n\n" +
			   $"Atk: {Atk}\n" +
			   $"ASpd: {AttackSpeed}\n" +
			   $"Hit: {Hit}\n" +
			   $"Avo: {Avo}\n" +
			   $"Crit: {Crit}\n" +
			   $"Ddg: {Ddg}\n";
	}
}
