using ComponentRegistrySystem;
using UnityEngine;

public abstract class Animal : MonoBehaviour
{
    public abstract string Introduce();
    void Awake() => ComponentRegistry.AutoRegistrate(this);
}
