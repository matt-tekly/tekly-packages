using System.Collections.Generic;
using System.Net;

namespace Tekly.Webster.Routes.Actions
{
	public class ActionDescriptor
	{
		public string Name;
		public string Group;
		public int ImageInstanceId = int.MaxValue;
	}
	
	public interface IActionHandler
	{
#if WEBSTER_ENABLE || UNITY_EDITOR && WEBSTER_ENABLE_EDITOR
		bool TryHandle(string route, HttpListenerRequest request, HttpListenerResponse response);
#endif
		IEnumerable<ActionDescriptor> GetDescriptors();
	}
}