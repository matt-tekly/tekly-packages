using System;
using UnityEngine;

namespace Tekly.Sheets.Dynamics
{
	public class DynamicConverterVector2 : DynamicConverter
	{
		public override object Convert(DynamicSerializer serializer, Type type, object dyn, object existing)
		{
			if (dyn is string str) {
				return StringToVector2(str);	
			}

			var dynamic = dyn as Dynamic;

			var result = existing != null ? (Vector3) existing : new Vector3();

			if (dynamic.TryGet("x", out object xObj)) {
				result.x = System.Convert.ToSingle(xObj);	
			}
			
			if (dynamic.TryGet("y", out object yObj)) {
				result.y = System.Convert.ToSingle(yObj);	
			}
			
			return result;
		}

		private static Vector2 StringToVector2(string str)
		{
			var sArray = str.Split(',');
			
			return new Vector3(
				float.Parse(sArray[0]),
				float.Parse(sArray[1])
			);
		}
	}
	
	public class DynamicConverterVector3 : DynamicConverter
	{
		public override object Convert(DynamicSerializer serializer, Type type, object dyn, object existing)
		{
			return Convert(dyn, existing);
		}

		public static Vector3 Convert(object dyn, object existing)
		{
			if (dyn is string str) {
				return StringToVector3(str);	
			}

			var dynamic = dyn as Dynamic;

			var result = existing != null ? (Vector3) existing : new Vector3();

			if (dynamic.TryGet("x", out object xObj)) {
				result.x = System.Convert.ToSingle(xObj);	
			}
			
			if (dynamic.TryGet("y", out object yObj)) {
				result.y = System.Convert.ToSingle(yObj);	
			}
			
			if (dynamic.TryGet("z", out object zObj)) {
				result.z = System.Convert.ToSingle(zObj);	
			}
			
			return result;
		}
		
		private static Vector3 StringToVector3(string str)
		{
			var sArray = str.Split(',');
			
			return new Vector3(
				float.Parse(sArray[0]),
				float.Parse(sArray[1]),
				float.Parse(sArray[2])
			);
		}
	}
	
	public class DynamicConverterQuaternion : DynamicConverter
	{
		public override object Convert(DynamicSerializer serializer, Type type, object dyn, object existing)
		{
			var eulerAngles = DynamicConverterVector3.Convert(dyn, new Vector3());
			return Quaternion.Euler(eulerAngles);
		}
	}
}