using System;
using System.Collections.Generic;

namespace ComponentRegistrySystem
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	public class RegistrableAttribute : Attribute
	{
	}
}