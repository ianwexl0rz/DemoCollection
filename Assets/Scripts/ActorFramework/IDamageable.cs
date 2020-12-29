using System;

public interface IDamageable
{
	float Health { get; set; }
	float MaxHealth { get; set; }
	event Action<float> OnHealthChanged;
	Action OnDestroyCallback { get; set; }

	void TakeDamage(float damage);

	void Destroy();
}
