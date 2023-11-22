using System;

namespace ComponentRegistrySystem
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	public class RegistrableAttribute : Attribute
	{
	}
}