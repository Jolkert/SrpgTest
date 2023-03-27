using Assets.Scripts;
using Assets.Scripts.Data;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Grid : MonoBehaviour
{
	[SerializeField] private int _width;
	[SerializeField] private int _height;
	[SerializeField] private Vector2 _topLeft;
	[SerializeField] private GameObject _tile;
	[SerializeField] private Sprite _forestSprite;
	[SerializeField] private GameObject _highlightBox;

	private GameObject[][] _tiles;
	private readonly List<Unit> _playerUnits = new List<Unit>();
	private readonly List<Unit> _enemyUnits = new List<Unit>();

	public int Width => _width;
	public int Height => _height;
	public Sprite ForestSprite => _forestSprite;
	public GameObject HighlightBox => _highlightBox;

	public Side Phase { get; private set; } = Side.Player;

	void Awake()
	{
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
	}

	public GridTile GetTile(int x, int y) => _tiles[x][y].GetComponent<GridTile>();
	public GridTile GetTile(Vector2Int position) => GetTile(position.x, position.y);

	public void RegisterUnit(Unit unit)
	{

	}
	public void OnUnitAction(Unit unit)
	{

	}

	public void SwitchPhase()
	{
		Phase = Phase == Side.Player ? Side.Enemy : Side.Player;

		foreach (var unit in _playerUnits.Concat(_enemyUnits))
			unit.ResetTurn();
	}

	public IEnumerable<GameObject> EnumerateTileObjects()
	{
		for (int i = 0; i < _tiles.Length; i++)
			for (int j = 0; j < _tiles[i].Length; j++)
				yield return _tiles[i][j];
	}
}
