using System;
using ComponentRegistrySystem;

[Registrable]
public interface IPredator
{
	Type GetFavoriteFood();
}