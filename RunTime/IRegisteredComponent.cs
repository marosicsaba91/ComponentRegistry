using System; 
using UnityEngine;

namespace ComponentRegistrySystem
{
public interface IRegisteredComponent
{
    event Action Enabled;
    event Action Disabled;
    event Action Destroyed;
    
    // Usage:
    
    /*    
        void Awake() => ComponentRegistry.RegistrateComponent(this);
        void OnEnable() => Enabled();
        void OnDisable() => Disabled();
        void OnDestroy() => Destroyed(); 
        
    // OR : 
        
        void Awake()
        {
            // Any other stuff
            ComponentRegistry.RegistrateComponent(this);
        }

        void OnEnable()
        {
            // Any other stuff
            Enabled();
        }

        void OnDisable()
        {
            // Any other stuff
            Disabled();
        }

        void OnDestroy()
        {
            // Any other stuff
            Destroyed();
        }            
     */
    
}

public static class ComponentHelper
{
    public static MonoBehaviour ToMonoBehaviour(this IRegisteredComponent registeredComponent) => (MonoBehaviour) registeredComponent;
    public static GameObject GetGameObject(this IRegisteredComponent registeredComponent) => ((MonoBehaviour) registeredComponent).gameObject; 
    public static Transform GetTransform(this IRegisteredComponent registeredComponent) => ((MonoBehaviour) registeredComponent).transform; 
}
}
