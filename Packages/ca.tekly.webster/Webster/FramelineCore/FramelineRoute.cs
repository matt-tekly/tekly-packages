//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using Tekly.Webster.Routing;

namespace Tekly.Webster.FramelineCore
{
	[Route("/api")]
	[RequestMainThread(false)]
	public class FramelineRoutes
	{
		[Hidden]
		[Get("/frameline")]
		public string GetFrameline()
		{
			return Frameline.Json;
		}

		[Hidden]
		[Delete("/frameline")]
		public void DeleteFrameline()
		{
			Frameline.Clear();
		}

		[Hidden]
		[Delete("/frameline/clearTo")]
		public void ClearTo(double time)
		{
			Frameline.ClearTo(time);
		}

		[Hidden]
		[Delete("/frameline/clearAfter")]
		public void ClearAfter(double time)
		{
			Frameline.ClearAfter(time);
		}
	}
}