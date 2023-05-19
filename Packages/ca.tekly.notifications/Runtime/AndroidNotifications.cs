#if UNITY_ANDROID
using Tekly.Logging;
using Unity.Notifications.Android;

namespace Tekly.Notifications
{
	public class AndroidNotifications : INotifications
	{
		private readonly AndroidNotificationChannel m_defaultChannel;
		private readonly TkLogger m_logger = TkLogger.Get<Notifications>();

		public AndroidNotifications()
		{
			m_defaultChannel = new AndroidNotificationChannel() {
				Id = "default_channel",
				Name = "Default Channel",
				Description = "Generic notifications",
				Importance = Importance.Default,
				EnableVibration = true
			};
			
			AndroidNotificationCenter.RegisterNotificationChannel(m_defaultChannel);
		}

		public void Schedule(NotificationData data)
		{
			var notification = new AndroidNotification();
			notification.Title = data.Title;
			notification.Text = data.Body;
			notification.FireTime = data.Time.DateTime;
			notification.ShowInForeground = true;
			notification.SmallIcon = "app_icon_small";
			notification.LargeIcon = "app_icon_large";
			
			Notifications.Log(data, m_logger);

			AndroidNotificationCenter.SendNotification(notification, m_defaultChannel.Id);
		}

		public void CancelAll()
		{
			AndroidNotificationCenter.CancelAllNotifications();
		}

		public void RequestPermission()
		{
			var _ = new PermissionRequest();
		}
	}
}
#endif