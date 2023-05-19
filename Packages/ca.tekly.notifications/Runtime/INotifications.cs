namespace Tekly.Notifications
{
	public interface INotifications
	{
		void Schedule(NotificationData data);
		void CancelAll();
		void RequestPermission();
	}

	public class NotificationsStub : INotifications
	{
		public void Schedule(NotificationData data) { }
		public void CancelAll() { }
		public void RequestPermission() { }
	}
}