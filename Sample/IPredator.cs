using System;
using ComponentDatabasesSystem;

[ComponentDatabaseType]
public interface IPredator  
{
    Type GetFavoriteFood(); 
}