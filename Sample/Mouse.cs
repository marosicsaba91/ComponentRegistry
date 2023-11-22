using ComponentRegistrySystem;

public interface ICatFood : IFood<Cat> { }

[Registrable]
public sealed class Mouse : Animal, ICatFood
{
	public override string Introduce() => "I'm Afraid!!!";
}

