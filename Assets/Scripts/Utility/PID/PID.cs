[System.Serializable]
public class PID
{
	public PIDConfig config = null;

	private float P, I, D;
	private float prevError;

	public PID(PIDConfig config)
	{
		this.config = config;
	}

	public float GetOutput(float currentError, float deltaTime)
	{
		P = currentError;
		I += P * deltaTime;
		D = (P - prevError) / deltaTime;
		prevError = currentError;

		return P * config.Kp + I * config.Ki + D * config.Kd;
	}
}
