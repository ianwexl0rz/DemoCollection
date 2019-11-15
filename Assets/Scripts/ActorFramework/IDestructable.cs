using System;

public interface IDestructable
{
	Action OnDestroyCallback { get; set; }
}
