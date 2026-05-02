using System;
using Tekly.DataModels.Models;
using UnityEngine;
using UnityEngine.Audio;

namespace TeklySample.Samples.Leaf
{
	[Serializable]
	public struct GameSettingsVolume
	{
		public string Parameter;
		public string Name;
	}
	
	public class GameSettingsSample : MonoBehaviour
	{
		[SerializeField] private AudioMixer m_mixer;
		[SerializeField] private GameSettingsVolume[] m_volumeSettings;

		private GameSettingsModel m_gameSettingsModel;
		
		private void Awake()
		{
			m_gameSettingsModel = new GameSettingsModel(m_mixer, m_volumeSettings);
			RootModel.Instance.Add("settings",  m_gameSettingsModel);
		}

		private void OnDestroy()
		{
			RootModel.Instance.RemoveModel("settings");
			m_gameSettingsModel.Dispose();
		}
	}
}