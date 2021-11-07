using System.Collections.Generic;
using ComponentRegistrySystem; 

public interface ICatFood : IFood<Cat> { }

public sealed class Mouse : Animal, ICatFood
{ 
    public override string Introduce() => "I'm Afraid!!!"; 
}
