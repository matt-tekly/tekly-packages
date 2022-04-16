//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

#if WEBSTER_ENABLE || UNITY_EDITOR && WEBSTER_ENABLE_EDITOR
using System;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using Tekly.Common.LifeCycles;
using Tekly.Webster.Routing;
using Tekly.Webster.Utility;
using UnityEngine;

namespace Tekly.Webster.Routes
{
	public class ScreenShotRoutes
	{
		[Hidden]
		[Get("/screenshot")]
		[RequestMainThread(false)]
		public void HandleScreenShot(HttpListenerResponse response)
		{
			byte[] bytes = null;
			
			WebsterServer.Dispatch(() => {
				LifeCycle.Instance.StartCoroutine(ScreenShot(pngBytes => bytes = pngBytes));
			});

			var stopWatch = new Stopwatch();
			stopWatch.Start();

			while (bytes == null) {
				Thread.Sleep(WebsterServer.SleepTimeMs);
				if (stopWatch.ElapsedMilliseconds > WebsterServer.TimeOutMs) {
					throw new Exception("ScreenShot timed out");
				}
			}

			stopWatch.Stop();

			response.ContentType = "image/png";
			response.ContentEncoding = Encoding.Default;

			response.WriteContent(bytes);
		}

		private static IEnumerator ScreenShot(Action<byte[]> onComplete)
		{
			// Ensure that the frame has actually fully rendered
			yield return new WaitForEndOfFrame();
			var texture = ScreenCapture.CaptureScreenshotAsTexture();
			onComplete(texture.EncodeToPNG());
		}
	}
}
#endif