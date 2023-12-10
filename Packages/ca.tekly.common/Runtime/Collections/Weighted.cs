using System;
using System.Collections.Generic;
using Tekly.Common.Utils;
using UnityEngine;

namespace Tekly.Common.Collections
{
	public interface IWeighted
	{
		int GetWeight();
	}
	
	[Serializable]
	public class WeightedValue<T> : IWeighted
	{
		public int GetWeight() => m_weight;
		public T Value => m_value;
		
		[SerializeField] private int m_weight;
		[SerializeField] private T m_value;
	}
	
	public static class WeightedRandom
	{
		public static T RandomWeighted<T>(this IList<T> weights, NumberGenerator numberGenerator) where T : IWeighted
		{
			if (weights == null || weights.Count == 0) {
				return default;
			}
			
			var count = weights.Count;
			
			var totalWeight = 0;
			
			for (var i = 0; i < count; i++) {
				totalWeight += weights[i].GetWeight();
			}
 
			var randomWeight = numberGenerator.Range(0, totalWeight);
  
			for (var i = 0; i < count; i++) {
				var weightedItem = weights[i];
				var weight = weightedItem.GetWeight();

				if (randomWeight < weight) {
					return weightedItem;
				}
                
				randomWeight -= weight;
			}
 
			return default;
		}
		
		public static T RandomWeighted<T>(this IList<T> weights, NumberGenerator numberGenerator, Func<T, int> weightSelector) 
		{
			if (weights == null || weights.Count == 0) {
				return default;
			}
			
			var count = weights.Count;
			
			var totalWeight = 0;
			
			for (var i = 0; i < count; i++) {
				totalWeight += weightSelector.Invoke(weights[i]);
			}
 
			var randomWeight = numberGenerator.Range(0, totalWeight);
  
			for (var i = 0; i < count; i++) {
				var weightedItem = weights[i];
				var weight = weightSelector.Invoke(weightedItem);

				if (randomWeight < weight) {
					return weightedItem;
				}
                
				randomWeight -= weight;
			}
 
			return default;
		}
	}
}