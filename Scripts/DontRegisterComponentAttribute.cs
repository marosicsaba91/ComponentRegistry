using System;
using System.Collections.Generic;

namespace ComponentRegistrySystem
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class DontRegisterComponentAttribute : Attribute
    {
        public readonly bool includeSubClasses;

        public DontRegisterComponentAttribute()
        {
            includeSubClasses = false;
        }

        public DontRegisterComponentAttribute(bool includeSubClasses)
        {
            this.includeSubClasses = includeSubClasses;
        }
    }

static class DontRegisterComponentHelper
    {
        internal static void RemoveIgnoredTypes(IList<Type> allAbstractSystemTypes)
        {
            for (int i = allAbstractSystemTypes.Count - 1; i >= 0; i--) 
                if(allAbstractSystemTypes[i].IsNonRegistrableComponent())
                    allAbstractSystemTypes.RemoveAt(i);
        }
        

        internal static bool IsNonRegistrableComponent(this Type type)
        {
            if (type.ContainsGenericParameters) return true;
            
            var inheritedAttribute = (DontRegisterComponentAttribute)
                Attribute.GetCustomAttribute(type, typeof(DontRegisterComponentAttribute), inherit: true);
            if (inheritedAttribute == null) return false;
            if (inheritedAttribute.includeSubClasses)
                return true;

            var attribute = (DontRegisterComponentAttribute)
                Attribute.GetCustomAttribute(type, typeof(DontRegisterComponentAttribute), inherit: false);
            return attribute != null;
        }
    }
}