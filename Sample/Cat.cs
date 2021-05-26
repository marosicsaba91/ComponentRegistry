using System;
using ComponentRegistrySystem;

[DontRegisterComponent]
public class Cat : Animal, IPet, IPredator
{
    public override string Introduce() => "Leave me alone.";
    public Type GetFavoriteFood() => typeof(Mouse);
    
}
