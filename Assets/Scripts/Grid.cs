using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Grid : MonoBehaviour
{
	[SerializeField] private int _width;
	[SerializeField] private int _height;
	[SerializeField] private Vector2 _topLeft;
	[SerializeField] private GameObject _tile;

	private GameObject[][] _tiles;

	public int Width { get => _width; }
	public int Height { get => _height; }

	void Start()
	{
		_tiles = new GameObject[Width][];
		// this is hella ugly lol -morgan 2023-03-24
		bool whiteRow = true;
		for (int i = 0; i < _tiles.Length; i++)
		{
			_tiles[i] = new GameObject[Height];
			for (int j = 0; j < _tiles[i].Length; j++)
			{
				int count = i * j;

				var tile = Instantiate(_tile, new Vector3(i * _tile.transform.localScale.x + _topLeft.x, j * _tile.transform.localScale.y - _topLeft.y), Quaternion.identity);
				if (whiteRow && j % 2 != 0 || !whiteRow && j % 2 == 0)
					tile.GetComponent<SpriteRenderer>().color = Color.black;

				tile.transform.SetParent(gameObject.transform, false);
				GridTile.AddTo(tile, new Vector2Int(i, j));

				_tiles[i][j] = tile;				
			}
			whiteRow = !whiteRow;
		}
	}

	public GridTile GetTile(int x, int y) => _tiles[x][y].GetComponent<GridTile>();
	public GridTile GetTile(Vector2Int position) => GetTile(position.x, position.y);

	public void SetTileColor(int x, int y, Color color) => _tiles[x][y].GetComponent<SpriteRenderer>().color = color;

	public IEnumerable<GameObject> EnumerateTiles()
	{
		for (int i = 0; i < _tiles.Length; i++)
			for (int j = 0; j < _tiles[i].Length; j++)
				yield return _tiles[i][j];
	}
}
