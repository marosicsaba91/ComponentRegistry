﻿using System;
using ComponentRegistrySystem;

[Registrable]
public class Crocodile : Animal, IPredator
{
    public override string Introduce() => "I'm Mr. Crock!";
    public Type GetFavoriteFood() => typeof(Dog); 
}