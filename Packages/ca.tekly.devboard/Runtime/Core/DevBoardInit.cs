using UnityEngine;

namespace Tekly.DevBoard
{
	public class DevBoardInit : MonoBehaviour
	{
		[SerializeField] private DevBoardAssets m_assets;
		
		private void Awake()
		{
			DevBoard.Instance.AddAssets(m_assets);
		}
	}
}