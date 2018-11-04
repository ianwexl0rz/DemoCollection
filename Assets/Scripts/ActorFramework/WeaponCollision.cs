using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class WeaponCollision
{
	private Vector3 origin, end, lastOrigin, lastEnd = Vector3.zero;
	private readonly List<Vector3> pointBuffer = new List<Vector3>();
	private readonly List<Color> colors = new List<Color>();

	public void SetInitialPosition(Vector3 p0, Vector3 p1)
	{
		lastOrigin = p0;
		lastEnd = p1;
	}

	public void SetCurrentPosition(Vector3 p0, Vector3 p1)
	{
		origin = p0;
		end = p1;
	}

	public void CheckHits(CombatActor attacker, float distThreshold)
	{
        float debugTime = Time.fixedDeltaTime * 8;

		attacker.CheckHit(origin, end);
        attacker.CheckHit(end, origin);
		attacker.CheckHit(lastOrigin, origin);

        Vector3 currentVector = end - origin;
        Vector3 lastVector = lastEnd - lastOrigin;

        int steps = 1 + (int)((currentVector - lastVector).magnitude / distThreshold);

        float colorRange = ((float)steps).LinearRemap(1f, 5f, 0.5f, 0f);
        Color color = Color.HSVToRGB(Mathf.Clamp01(colorRange), 1, 1);

		Vector3[] addPoints = new Vector3[steps * 2];
		Color[] addColors = new Color[steps * 2];

		for(int i = 0; i < steps; i++)
        {
			float t = (i + 1f) / steps;

            Vector3 blendedOrigin = Vector3.Lerp(lastOrigin, origin, t);
            Vector3 blendedEnd = blendedOrigin + Vector3.Slerp(lastVector, currentVector, t);
			attacker.CheckHit(blendedOrigin, blendedEnd);

	        addPoints[i * 2] = blendedOrigin;
	        addPoints[i * 2 + 1] = blendedEnd;

	        addColors[i * 2] = addColors[i * 2 + 1] = Color.white; //color;

			Debug.DrawLine(blendedOrigin, blendedEnd, color, debugTime);
	        Debug.DrawLine(i == 0 ? lastEnd : addPoints[i * 2 - 1], blendedEnd, color, debugTime);
	        Debug.DrawLine(i == 0 ? lastOrigin : addPoints[i * 2 - 2], blendedOrigin, color, debugTime);
		}

		if(pointBuffer.Count == 0)
		{
			Vector3[] lastPoints =
			{
				lastOrigin,
				lastEnd
				//attacker.transform.InverseTransformPoint(lastOrigin),
				//attacker.transform.InverseTransformPoint(lastEnd),
			};

			pointBuffer.AddRange(lastPoints);
			Debug.DrawLine(lastOrigin, lastEnd, color, debugTime);

			Color[] lastColors = { color, color };
			colors.AddRange(lastColors);
		}

		pointBuffer.AddRange(addPoints);
		colors.AddRange(addColors);

		var localPoints = new List<Vector3>(pointBuffer);
		for(var i = localPoints.Count; i-- > 0;)
		{
			localPoints[i] = attacker.transform.InverseTransformPoint(localPoints[i]);
		}

		SetInitialPosition(origin, end);

		if(attacker.weaponTrail != null)
			attacker.weaponTrail.UpdateAndShowMesh(localPoints, colors);
	}

	public void ClearWeaponTrail(CombatActor attacker)
	{
		pointBuffer.Clear();
		colors.Clear();
		if(attacker.weaponTrail != null)
			attacker.weaponTrail.HideMesh();
	}
}
