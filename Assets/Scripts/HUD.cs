using UnityEngine;

public class HUD : MonoBehaviour
{
	public RectTransform healthBarFill;
	public GameObject pauseOverlay;

	public void OnUpdate()
	{
		Player player = GameManager.I.activePlayer;

		if(player == null) return;

		if(Input.GetKeyDown(KeyCode.RightBracket))
		{
			player.health = Mathf.Min(player.health + 5f, player.maxHealth);
		}

		if(Input.GetKeyDown(KeyCode.LeftBracket))
		{
			player.health = Mathf.Max(player.health - 5f, 0f);
		}

		// TODO: Only update this when it changes
		healthBarFill.anchorMax = new Vector2(player.health / player.maxHealth, healthBarFill.anchorMax.y);
	}

	public void SetPaused(bool value)
	{
		pauseOverlay.SetActive(value);
	}
}