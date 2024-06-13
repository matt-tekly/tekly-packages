using DotLiquid;
using UnityEngine;

namespace Tekly.Tinker.Core
{
	
	public class BaseDrop
	{
		public string GreetingBase => "BaseBall";
	}
	
	public class TestDrop : BaseDrop
	{
		public string Greeting => "Hey, how are ya?";
	}
	
	[Route("/tinker")]
	public class TinkerPages
	{
		[Page("/home", "tinker_home", "page")]
		public TestDrop Home()
		{
			return new TestDrop();
		}
		
		[Page("/routes", "tinker_routes")]
		public TestDrop Routes()
		{
			return new TestDrop();
		}
		
		[Page("/void", "tinker_home")]
		public void Void()
		{
			Debug.Log("Yes");
		}
	}
}