using Tekly.Logging;

namespace Tekly.Notifications
{
	public class Notifications
	{
		public static INotifications CreateImpl()
		{
#if UNITY_IOS
			return new IosNotifications();
#elif UNITY_ANDROID	
			return new AndroidNotifications();
#else
			return new NotificationsStub();
#endif
		}

		public static void Log(NotificationData data, TkLogger logger)
		{
			logger.Debug($"[{data.Time.ToLocalTime()}] [{data.Title}] [{data.Body}]");
		}
	}
}