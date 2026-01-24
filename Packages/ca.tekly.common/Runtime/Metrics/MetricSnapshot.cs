namespace Tekly.Common.Metrics
{
	public readonly struct MetricSnapshot
	{
		public readonly MetricKey Key;
		public readonly long Count;
		public readonly double Amount;

		public MetricSnapshot(MetricKey key, long count, double amount)
		{
			Key = key;
			Count = count;
			Amount = amount;
		}

		public override string ToString()
		{
			return $"{Key.Path} count={Count} amount={Amount}";
		}
	}
}