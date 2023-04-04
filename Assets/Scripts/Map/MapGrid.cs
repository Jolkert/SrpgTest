using Assets.Scripts;
using Assets.Scripts.Data;
using Assets.Scripts.Extensions;
using Assets.Scripts.Structure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

#nullable enable
public class MapGrid : MonoBehaviour, IEnumerable<MapTile>, IEnumerable
{
	// static
	public static readonly Vector2Int[] NeighborOffsets = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
	public static readonly Color TransparentColor = new Color(1, 1, 1, 0);
	public static readonly Color SelectionBoxColor = ColorCreator.FromHexString("#fffd8f");

	// instance
	[SerializeField] private int _width;
	public int Width => _width;
	[SerializeField] private int _height;
	public int Height => _height;
	private Vector2Int _gridOffset;

	[SerializeField] private TextAsset _dataJson = null!;

	[SerializeField] private TextMeshProUGUI _tileText = null!;
	[SerializeField] private TextMeshProUGUI _unitText = null!;
	[SerializeField] private TextMeshProUGUI _phaseText = null!;

	private MapTile[][] _tiles = null!;
	public MapTile this[int x, int y] => GetTile(x, y);
	public MapTile this[Vector2Int coodrinates] => this[coodrinates.x, coodrinates.y];

	public Tilemap BackgroundSprites { get; private set; } = null!;
	public Tilemap HighlightSprites { get; private set; } = null!;
	public Tilemap SelectionSprites { get; private set; } = null!;

	private Vector2Int _previousSelectedTile;
	public Unit? SelectedUnit { get; set; } = null;
	public Side Phase { get; private set; } = Side.Player;
	private readonly Dictionary<Side, List<Unit>> _units = new Dictionary<Side, List<Unit>>();
	private IEnumerable<Unit> AllUnits
	{
		get
		{
			foreach ((Side _, List<Unit> list) in _units)
				foreach (Unit unit in list)
					yield return unit;
		}
	}

	// Init
	private void Awake()
	{// Awake() runs before Start() so we initialize the outputGrid before the units -morgan 2023-03-27
		BackgroundSprites = this.FindComponentInChildren<Tilemap>("map_background");
		HighlightSprites = this.FindComponentInChildren<Tilemap>("map_highlight");
		SelectionSprites = this.FindComponentInChildren<Tilemap>("unit_foreground");
		_gridOffset = new Vector2Int(Width / 2, Height / 2);

		InitializeTiles();

		// init text
		_phaseText.text = "Player Phase";
	}
	private void InitializeTiles()
	{
		MapTile.MapTileData[][] data = MapTile.CreateGridFromJson(_dataJson.text);

		_tiles = new MapTile[Width][];
		for (int i = 0; i < _tiles.Length; i++)
		{
			_tiles[i] = new MapTile[Height];
			for (int j = 0; j < _tiles[i].Length; j++)
			{
				Vector2Int mapPosition = new Vector2Int(i, j);
				Vector3Int worldGridPosition = MapToWorldCell(mapPosition);

				// background
				_tiles[i][j] = new MapTile(mapPosition, data[i][j]);

				// highlight
				HighlightSprites.UnsetTileFlags(worldGridPosition, TileFlags.LockColor);
				ResetTileHighlight(mapPosition);

				// selection
				SelectionSprites.UnsetTileFlags(worldGridPosition, TileFlags.LockColor);
				SelectionSprites.SetColor(worldGridPosition, TransparentColor);
			}
		}
	}
	public void RegisterUnit(Unit unit)
	{
		if (!_units.ContainsKey(unit.Side))
			_units.Add(unit.Side, new List<Unit>() { unit });
		else
			_units[unit.Side].Add(unit);

		GetTile(unit.Position).OccupyingUnit = unit;
	}

	// Player input
	private void Update()
	{
		ProcessKeyInput();
		ProcessDebugKeyInput();
	}

	void ProcessKeyInput()
	{
		if (SelectedUnit == null)
			return;

		if (SelectedUnit.IsAwaitingAction)
		{
			if (Input.GetKeyUp(KeyCode.Escape) || Input.GetMouseButtonUp((int)MouseButton.Right))
				SelectedUnit.UndoMovement();
			else if (Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.KeypadEnter))
				SelectedUnit.WaitAction();
		}
		else if (Input.GetKeyUp(KeyCode.Escape) || Input.GetMouseButtonUp((int)MouseButton.Right))
			SelectedUnit.SetSelected(false);


	}
	void ProcessDebugKeyInput()
	{
		if (Input.GetKeyUp(KeyCode.F3))
			Debug.Log($"Queue RN: {SharedResources.Random.Generate()}");

		if (Input.GetKeyUp(KeyCode.F4))
			Debug.Log($"Burn RN: {SharedResources.Random.Next()}");
	}

	private Vector2Int _mouseDownTilePos = new Vector2Int(-1, -1);
	private void OnMouseDown() => _mouseDownTilePos = ScreenToMapPosition(Input.mousePosition);
	private void OnMouseUp()
	{
		if (_mouseDownTilePos == null)
			return;

		if (_mouseDownTilePos == ScreenToMapPosition(Input.mousePosition) && PositionInBounds(_mouseDownTilePos))
			PlayerInteractTile(GetTile(_mouseDownTilePos));

		_mouseDownTilePos = new Vector2Int(-1, -1);
	}
	private void PlayerInteractTile(MapTile tile)
	{
		if (SelectedUnit == null)
		{
			if (tile.OccupyingUnit != null && !tile.OccupyingUnit.HasMoved && Phase == tile.OccupyingSide)
				tile.OccupyingUnit.SetSelected(true);

			return;
		}

		if (SelectedUnit.IsAwaitingAction &&
			SelectedUnit.IsOpponentTo(tile.OccupyingSide) &&
			SelectedUnit.AwaitingAttackRange!.Contains(tile))
		{
			SelectedUnit.InitiateCombatWith(tile.OccupyingUnit!);
			return;
		}

		if (!SelectedUnit.IsAwaitingAction && (SelectedUnit == tile.OccupyingUnit || SelectedUnit.CanMoveTo(tile)))
		{
			SelectedUnit.AwaitActionAt(tile);
			return;
		}
	}

	private void OnMouseOver()
	{
		if (TryGetTile(ScreenToMapPosition(Input.mousePosition), out MapTile? tile))
			MouseOverTile(tile);
	}
	private void MouseOverTile(MapTile tile)
	{
		if (tile.OccupyingUnit != null)
			SetUnitText(tile.OccupyingUnit.ToDebugString());
		else
			SetUnitText("No unit selected");

		SetTileText(tile.ToDebugString());

		MoveSelectionBoxToTile(tile.Position);
	}

	private void MoveSelectionBoxToTile(Vector2Int position)
	{
		SelectionSprites.SetColor(MapToWorldCell(_previousSelectedTile), TransparentColor);
		SelectionSprites.SetColor(MapToWorldCell(position), SelectionBoxColor);
		_previousSelectedTile = position;
	}

	public void OnUnitAction(Unit triggerUnit)
	{
		foreach (Unit unit in AllUnits)
			unit.ResetValidMoves();

		bool allMoved = true;
		foreach (Unit unit in _units[triggerUnit.Side])
		{
			if (!unit.HasMoved)
			{
				allMoved = false;
				break;
			}
		}

		if (allMoved)
			SwitchPhase();
	}

	// State handling
	public void SwitchPhase()
	{
		Phase = Phase == Side.Player ? Side.Enemy : Side.Player;
		_phaseText.text = $"{Phase} Phase";

		foreach (Unit unit in AllUnits)
			unit.ResetTurn();
	}
	public void SetTileText(string text)
	{
		_tileText.text = text;
	}
	public void SetUnitText(string text)
	{
		_unitText.text = text;
	}

	// Tile retreival & Modification
	public MapTile GetTile(Vector2Int position) => GetTile(position.x, position.y);
	public MapTile GetTile(int x, int y) => _tiles[x][y];
	public IEnumerable<MapTile> GetNeighborsOf(MapTile tile)
	{
		foreach (Vector2Int offset in NeighborOffsets)
			if (TryGetTile(tile.Position + offset, out MapTile? neighbor))
				yield return neighbor;
	}
	public IEnumerable<MapTile> GetTilesWithinRangeOf(MapTile tile, Weapon.WeaponRange range)
	{
		if (range == 1)
		{
			foreach (MapTile neighbor in GetNeighborsOf(tile))
				yield return neighbor;

			yield break;
		}

		UniqueQueue<(MapTile tile, int depth)> processingQueue = new();
		List<MapTile> visited = new();

		processingQueue.Enqueue((tile, 0));
		while (processingQueue.Count > 0)
		{
			(MapTile current, int depth) = processingQueue.Dequeue();

			if (range.Contains(depth))
				yield return current;

			if (depth < range.Max)
			{
				if (!visited.Contains(current))
					visited.Add(current);

				foreach (MapTile neighbor in GetNeighborsOf(current))
					if (!visited.Contains(neighbor))
						processingQueue.Enqueue((neighbor, depth + 1));
			}
		}
	}

	public bool PositionInBounds(Vector2Int position) => PositionInBounds(position.x, position.y);
	public bool PositionInBounds(int x, int y) => (x >= 0 && y >= 0 && x < _width && y < _height);

	public bool TryGetTile(Vector2Int position, [NotNullWhen(true)] out MapTile? tile) => TryGetTile(position.x, position.y, out tile);
	public bool TryGetTile(int x, int y, [NotNullWhen(true)] out MapTile? tile)
	{
		bool inBounds = PositionInBounds(x, y);
		tile = inBounds ? GetTile(x, y) : null;
		return inBounds;
	}

	public void HighlightTile(Vector2Int position, RangeType type) => HighlightTile(position, MapTile.HighlightColors[type]);
	public void HighlightTile(Vector2Int position, Color color)
	{
		HighlightSprites.SetColor(MapToWorldCell(position), color);
	}

	public void HighlightTiles(IEnumerable<Vector2Int> positions, RangeType type) => positions.ForEach(pos => HighlightTile(pos, type));
	public void HighlightTIles(IEnumerable<Vector2Int> positions, Color color) => positions.ForEach(pos => HighlightTile(pos, color));

	public void ResetTileHighlight(Vector2Int tile) => HighlightTile(tile, TransparentColor);
	public void ResetAllHighlights()
	{
		foreach (MapTile tile in this)
			ResetTileHighlight(tile.Position);
	}

	private Vector2Int ScreenToMapPosition(Vector3 screenPosition) => ((Vector2Int)BackgroundSprites.WorldToCell(Camera.main.ScreenToWorldPoint(screenPosition))) + _gridOffset;
	private Vector3Int MapToWorldCell(Vector2Int mapCellPosition) => (Vector3Int)(mapCellPosition - _gridOffset);
	public Vector3 MapCellToWorld(Vector2Int mapCellPosition) => BackgroundSprites.CellToWorld((Vector3Int)(mapCellPosition - _gridOffset)) + BackgroundSprites.cellSize / 2;

	// IEnumerable
	public void ForEach(Action<MapTile> action)
	{
		foreach (MapTile tile in this)
			action(tile);
	}
	#region IEnumerable<GridTile> implementation
	public IEnumerator<MapTile> GetEnumerator() => new GridTileEnumerator(this);
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public class GridTileEnumerator : IEnumerator<MapTile>, IEnumerator
	{
		private readonly MapGrid _grid;
		private int _currentX = 0, _currentY = -1;
		private MapTile[][] Tiles => _grid._tiles;

		public MapTile Current { get; private set; }
		object IEnumerator.Current => Current;


		public GridTileEnumerator(MapGrid grid)
		{
			_grid = grid;
			Current = null!;
		}


		public bool MoveNext()
		{
			if (++_currentY >= Tiles[_currentX].Length)
			{
				_currentX++;
				_currentY = 0;
			}

			if (_currentX >= Tiles.Length)
				return false;

			Current = _grid.GetTile(_currentX, _currentY);
			return true;
		}

		public void Reset()
		{
			_currentX = 0;
			_currentY = 0;
			Current = _grid.GetTile(0, 0);
		}

		public void Dispose()
		{
			_currentX = -1;
			_currentY = -1;
		}
	}
	#endregion
}
