using ComponentDatabasesSystem;
using UnityEngine;

[ComponentDatabaseType]
public abstract class Animal : MonoBehaviour
{
    public abstract string Introduce();

    void Awake()
    {
        ComponentDatabase.global.AddComponent(this);
    } 
}