using UnityEngine;

public class PauseMenu : MonoBehaviour
{
	public void Show(bool value) => gameObject.SetActive(value);
}