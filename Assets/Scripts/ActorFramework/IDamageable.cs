using System;

public interface IDamageable
{
	float Health { get; set; }
	float MaxHealth { get; set; }
	Action<float> OnHealthChanged { get; set; }
	Action OnDestroyCallback { get; set; }

	void Destroy();
}
