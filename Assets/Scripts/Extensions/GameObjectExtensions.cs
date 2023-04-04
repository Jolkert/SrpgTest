using UnityEngine;
using UnityEngine.Tilemaps;

namespace Assets.Scripts.Extensions
{
	public static class GameObjectExtensions
	{
		public static T FindComponentInChildren<T>(this MonoBehaviour self, string childTag) where T : Component
		{
			foreach (Transform child in self.transform)
			{
				if (child.CompareTag(childTag))
					return child.GetComponent<T>();
			}

			return null;
		}
		public static TileBase GetTile(this Tilemap self, Vector2Int position) => self.GetTile((Vector3Int)position);
		public static void SetColor(this Tilemap self, Vector2Int position, Color color) => self.SetColor((Vector3Int)position, color);

		public static void UnsetTileFlags(this Tilemap self, Vector3Int position, TileFlags flags)
		{
			TileFlags currentFlags = self.GetTileFlags(position);
			self.SetTileFlags(position, currentFlags & ~flags);
		}
	}
}
