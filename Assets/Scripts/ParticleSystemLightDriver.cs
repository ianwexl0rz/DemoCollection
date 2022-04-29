using System;
using UnityEngine;

public class ParticleSystemLightDriver : MonoBehaviour
{
	public float offsetCloserToCamera = 0f;
	public new Light light = null;
	public AnimationCurve lightCurve = new AnimationCurve();
	public float lightIntensity = 1f;

	private ParticleSystem _ps;
	private Vector3 _originalPos;
	private Camera _mainCamera;

	private void Awake()
	{
		_ps = GetComponent<ParticleSystem>();
		_originalPos = transform.position;
		_mainCamera = GameManager.Camera.GetComponent<Camera>();
		PauseMode.OnPauseGame += ToggleParticlePlayback;
	}

	private void OnDestroy()
	{
		PauseMode.OnPauseGame -= ToggleParticlePlayback;
	}

	private void ToggleParticlePlayback(bool paused)
	{
		if (paused) _ps.Pause(); else _ps.Play();
	}

	private void Update ()
	{
		if (_ps.isPlaying)
		{
			var t = _ps.time / _ps.main.duration;
			light.color = _ps.colorOverLifetime.color.Evaluate(t);
			light.intensity = lightIntensity * lightCurve.Evaluate(t);
		}

		if(offsetCloserToCamera > 0f)
		{
			var cameraToParticle = _mainCamera.transform.position - _originalPos;
			transform.position = _originalPos + cameraToParticle.normalized * offsetCloserToCamera;
		}
	}
}
