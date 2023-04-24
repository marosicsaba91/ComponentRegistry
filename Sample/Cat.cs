using System;
using ComponentRegistrySystem;

[Registrable]
public class Cat : Animal, IPet, IPredator
{
	public override string Introduce() => "Leave me alone.";
	public Type GetFavoriteFood() => typeof(Mouse);

}
