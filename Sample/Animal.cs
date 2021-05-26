using System; 
using ComponentRegistrySystem;
using UnityEngine;

public abstract class Animal : MonoBehaviour, IRegisteredComponent
{
    public abstract string Introduce();

    public event Action Enabled;
    public event Action Disabled;
    public event Action Destroyed;

    void Awake() => ComponentRegistry.RegistrateComponent(this);
    void OnEnable() => Enabled();
    void OnDisable() => Disabled();
    void OnDestroy() => Destroyed();
}
