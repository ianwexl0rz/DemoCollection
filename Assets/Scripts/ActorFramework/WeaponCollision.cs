using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class WeaponCollision
{
	public Vector3 origin, end, lastOrigin, lastEnd = Vector3.zero;

    public List<Vector3> pointBuffer = new List<Vector3>();

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

	public void CheckHits(CombatCharacter attacker, float distThreshold)
	{
        float debugTime = Time.fixedDeltaTime * 8;

		attacker.CheckHit(origin, end);
        attacker.CheckHit(end, origin);
		attacker.CheckHit(lastOrigin, origin);

        Vector3 currentVector = end - origin;
        Vector3 lastVector = lastEnd - lastOrigin;

        int steps = (int)((currentVector - lastVector).magnitude / distThreshold);
        Vector3[] points = new Vector3[steps + 1];
        points[steps] = end;

        float colorRange = ((float)steps).LinearRemap(0f, 5f, 0.5f, 0f);
        Color color = Color.HSVToRGB(Mathf.Clamp01(colorRange), 1, 1);
        
        for(int i = steps; i-- > 0;)
        {
			float t = (i + 1f) / (steps + 1f);

            Vector3 blendedOrigin = Vector3.Lerp(lastOrigin, origin, t);
            Vector3 relativeEnd = Vector3.Slerp(lastVector, currentVector, t);

            points[i] = relativeEnd + blendedOrigin;
			attacker.CheckHit(blendedOrigin, points[i]);
            
            Debug.DrawLine(blendedOrigin, points[i], color, debugTime);
            Debug.DrawLine(points[i], points[i+1], color, debugTime);
            
            if(i == 0)
            {
                Debug.DrawLine(lastEnd, points[i], color, debugTime);
            }
        }

        if(steps == 0)
        {
            Debug.DrawLine(lastEnd, end, color, debugTime);
        }

        Debug.DrawLine(lastOrigin, lastEnd, color, debugTime);
        Debug.DrawLine(origin, end, color, debugTime);
        Debug.DrawLine(lastOrigin, origin, color, debugTime);

        SetInitialPosition(origin, end);
        pointBuffer.AddRange(points);
	}
}
