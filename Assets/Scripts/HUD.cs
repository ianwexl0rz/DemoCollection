using UnityEngine;

public class HUD : MonoBehaviour
{
	public RectTransform healthBarFill;
	public GameObject pauseOverlay;

	public void RegisterPlayer(Actor actor)
	{
		actor.OnHealthChanged += UpdateHealthBar;
		UpdateHealthBar(actor.Health / actor.maxHealth);
	}
	
	public void UnregisterPlayer(Actor actor) => actor.OnHealthChanged -= UpdateHealthBar;

	private void UpdateHealthBar(float normalizedHealth) => healthBarFill.anchorMax = new Vector2(normalizedHealth, healthBarFill.anchorMax.y);

	// TODO: Pause overlay and health bar should not be bundled into one general HUD script.
	public void SetPaused(bool value) => pauseOverlay.SetActive(value);
}