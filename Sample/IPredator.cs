using System;
using ComponentRegistrySystem;

public interface IPredator : IRegisteredComponent 
{
    Type GetFavoriteFood(); 
}