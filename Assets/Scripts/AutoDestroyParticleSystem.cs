using UnityEngine;

public class AutoDestroyParticleSystem : MonoBehaviour {

	public bool destroyOnComplete = false;
	public float offsetCloserToCamera = 0f;
	public new Light light = null;
	public AnimationCurve lightCurve = new AnimationCurve();
	public float lightIntensity = 1f;

	private ParticleSystem ps;
	private Vector3 originalPos;

	private void OnEnable()
	{
		ps = GetComponent<ParticleSystem>();
		originalPos = transform.position;
		GameManager.I.OnPauseGame += ToggleParticlePlayback;
	}

	private void OnDisable()
	{
		if(GameManager.I != null)
		{
			GameManager.I.OnPauseGame -= ToggleParticlePlayback;
		}
	}

	void ToggleParticlePlayback(bool paused)
	{
		if(paused) ps.Pause(); else ps.Play();
	}

	void Update ()
	{
		if(ps.isPlaying)
		{
			float t = ps.time / ps.main.duration;
			light.color = ps.colorOverLifetime.color.Evaluate(t);
			light.intensity = lightIntensity * lightCurve.Evaluate(t);
		}

		if(offsetCloserToCamera > 0f)
		{
			Vector3 dir = Camera.main.transform.position - originalPos;
			transform.position = originalPos + dir.normalized * offsetCloserToCamera;
		}

		if(destroyOnComplete && !ps.IsAlive())
		{
			Destroy(gameObject);
		}
	}
}
