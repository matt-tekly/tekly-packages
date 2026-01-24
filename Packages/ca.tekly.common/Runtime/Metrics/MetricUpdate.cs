namespace Tekly.Common.Metrics
{
	public readonly struct MetricUpdate
	{
		public readonly MetricKey Key;

		public readonly long Count;
		public readonly double Amount;

		public readonly long CountDelta;
		public readonly double AmountDelta;

		public MetricUpdate(MetricKey key, long count, double amount, long countDelta, double amountDelta) {
			Key = key;
			Count = count;
			Amount = amount;
			CountDelta = countDelta;
			AmountDelta = amountDelta;
		}

		public override string ToString()
		{
			return $"{Key.Path} count={Count} amount={Amount} (Δ count={CountDelta}, Δ amount={AmountDelta}";
		}
	}
}