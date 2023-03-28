using UnityEngine;

public class EndTurnButton : MonoBehaviour
{
	[SerializeField] private Grid _map;

	private void OnMouseUpAsButton() => _map.SwitchPhase();
}