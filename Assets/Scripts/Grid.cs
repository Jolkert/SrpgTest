using Assets.Scripts;
using Assets.Scripts.Data;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

#nullable enable
public class Grid : MonoBehaviour, IEnumerable<GridTile>, IEnumerable
{
	[SerializeField] private int _width;
	[SerializeField] private int _height;
	[SerializeField] private Vector2 _topLeft;
	[SerializeField] private GameObject _tile = null!;
	[SerializeField] private Sprite _forestSprite = null!;
	[SerializeField] private GameObject _highlightBox = null!;

	[SerializeField] private TextMeshProUGUI _debugText = null!;
	[SerializeField] private TextMeshProUGUI _phaseText = null!;

	private GameObject[][] _tiles = null!;
	private readonly List<Unit> _playerUnits = new List<Unit>();
	private readonly List<Unit> _enemyUnits = new List<Unit>();
	private IEnumerable<Unit> AllUnits => _playerUnits.Concat(_enemyUnits);

	public int Width => _width;
	public int Height => _height;
	public Sprite ForestSprite => _forestSprite;
	public GameObject HighlightBox => _highlightBox;

	public Side Phase { get; private set; } = Side.Player;

	void Awake()
	{// Awake() runs before Start() so we initialize the grid before the units -morgan 2023-03-27
	 // init size & grid
		_tiles = new GameObject[Width][];
		for (int i = 0; i < _tiles.Length; i++)
		{
			_tiles[i] = new GameObject[Height];
			for (int j = 0; j < _tiles[i].Length; j++)
			{
				GameObject tile = Instantiate(_tile, new Vector3(i * _tile.transform.localScale.x + _topLeft.x, j * _tile.transform.localScale.y - _topLeft.y), Quaternion.identity);

				tile.transform.SetParent(gameObject.transform, false);
				GridTile.AddTo(tile, new Vector2Int(i, j));

				_tiles[i][j] = tile;
			}
		}

		// init unit lists
		GameObject.FindGameObjectsWithTag("unit_player")
					.Select(obj => obj.GetComponent<Unit>())
					.ForEach(unit => _playerUnits.Add(unit));
		GameObject.FindGameObjectsWithTag("unit_enemy")
					.Select(obj => obj.GetComponent<Unit>())
					.ForEach(unit => _enemyUnits.Add(unit));

		// init text
		SetDebugText("Initialized!");
		_phaseText.text = "Player Phase";
	}

	public GridTile GetTile(Vector2Int position) => GetTile(position.x, position.y);
	public GridTile GetTile(int x, int y) => _tiles[x][y].GetComponent<GridTile>();

	public bool PositionInBounds(Vector2Int position) => PositionInBounds(position.x, position.y);
	public bool PositionInBounds(int x, int y) => x >= 0 && y >= 0 && x < _width && y < _height;

	public bool TryGetTile(Vector2Int position, [NotNullWhen(true)] out GridTile? tile)
	{
		return TryGetTile(position.x, position.y, out tile);
	}
	public bool TryGetTile(int x, int y, [NotNullWhen(true)] out GridTile? tile)
	{
		bool inBounds = PositionInBounds(x, y);
		tile = inBounds ? GetTile(x, y) : null;
		return inBounds;
	}

	public void OnUnitMove(Unit triggerUnit)
	{
		foreach (Unit unit in AllUnits)
			unit.ResetValidMoves();

		List<Unit> inPhaseUnits = triggerUnit.Side == Side.Player ? _playerUnits : _enemyUnits;
		bool allMoved = true;
		foreach (Unit unit in inPhaseUnits)
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

	public void SwitchPhase()
	{
		Phase = Phase == Side.Player ? Side.Enemy : Side.Player;
		_phaseText.text = $"{Phase} Phase";

		foreach (Unit? unit in _playerUnits.Concat(_enemyUnits))
			unit.ResetTurn();
	}

	public void SetDebugText(string text)
	{
		_debugText.text = text;
	}


	#region IEnumerable<GridTile> implementation
	public IEnumerator<GridTile> GetEnumerator() => new GridTileEnumerator(this);
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public class GridTileEnumerator : IEnumerator<GridTile>, IEnumerator
	{
		private readonly Grid _grid;
		private int _currentX = 0, _currentY = 0;
		private GameObject[][] Tiles => _grid._tiles;

		public GridTile Current { get; private set; }
		object IEnumerator.Current => Current;


		public GridTileEnumerator(Grid grid)
		{
			_grid = grid;
			Current = _grid.GetTile(0, 0);
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
