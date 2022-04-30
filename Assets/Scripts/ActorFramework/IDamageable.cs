using System;
using ActorFramework;

public interface IDamageable
{
	public Health Health { get; }

	public void Die();
}
