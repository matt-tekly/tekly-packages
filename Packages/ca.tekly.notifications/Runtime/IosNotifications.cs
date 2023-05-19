#if UNITY_IOS
using Tekly.Logging;
using Unity.Notifications.iOS;

namespace Tekly.Notifications
{
	public class IosNotifications : INotifications
	{
		private readonly TkLogger m_logger = TkLogger.Get<Notifications>();

		public void Schedule(NotificationData notificationData)
		{
			var utcTime = notificationData.Time.UtcDateTime;
			
			var calendarTrigger = new iOSNotificationCalendarTrigger {
				Year = utcTime.Year,
				Day = utcTime.Day,
				Month = utcTime.Month,
				Hour = utcTime.Hour,
				Minute = utcTime.Minute,
				Second = utcTime.Second,
				UtcTime = true
			};

			var notification = new iOSNotification {
				Title = notificationData.Title,
				Body = notificationData.Body,
				Trigger = calendarTrigger,
				ShowInForeground = false
			};
			
			Notifications.Log(notificationData, m_logger);
			iOSNotificationCenter.ScheduleNotification(notification);
		}

		public void CancelAll()
		{
			iOSNotificationCenter.RemoveAllScheduledNotifications();
		}

		public void RequestPermission()
		{
		}
	}
}
#endif