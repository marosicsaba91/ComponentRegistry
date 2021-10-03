using System;
using ComponentDatabasesSystem;

public abstract class Dog : Animal, IPet, IPredator
{ 
    public override string Introduce() => "My name is Boy, Very G. Boy.";
    public Type GetFavoriteFood() => typeof(Cat); 
}
