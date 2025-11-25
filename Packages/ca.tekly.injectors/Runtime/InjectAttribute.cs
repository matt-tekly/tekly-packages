// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using JetBrains.Annotations;
using UnityEngine.Scripting;

namespace Tekly.Injectors
{
	public enum IsOptional
	{
		Required,
		Optional
	}
	
	[MeansImplicitUse(ImplicitUseKindFlags.Assign)]
	public class InjectAttribute : PreserveAttribute
	{
		public readonly string Id;
		public readonly IsOptional IsOptional;

		public InjectAttribute() { }
		
		public InjectAttribute(string id = null)
		{
			Id = id;
		}
		
		public InjectAttribute(IsOptional optional)
		{
			IsOptional = optional;
		}
	}
	
	[MeansImplicitUse(ImplicitUseKindFlags.Assign)]
	public class InjectOptionalAttribute : InjectAttribute
	{
		public InjectOptionalAttribute() : base(IsOptional.Optional)
		{
			
		}
	}
}