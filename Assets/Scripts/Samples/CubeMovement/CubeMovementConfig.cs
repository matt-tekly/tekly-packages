using UnityEngine;

namespace TeklySample.Samples.CubeMovement
{
	public class CubeMovementConfig : MonoBehaviour
	{
		public float Size;
		public int Rows = 10;
		public int Columns = 10;
		public int Planes = 1;
		
		public int TotalCubes => Rows * Columns * Planes;
	}
}