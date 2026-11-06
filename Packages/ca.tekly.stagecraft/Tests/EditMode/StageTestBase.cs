using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tekly.Injectors;
using Tekly.Logging;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tekly.Stagecraft.Tests
{
	public abstract class StageTestBase
	{
		protected Stage Stage;
		protected TestSystem System;
		protected InjectorContainer Container;

		protected int Errors => m_errors;
		protected string LastError => m_lastError;

		private readonly List<GameObject> m_prefabs = new List<GameObject>();
		private int m_errors;
		private string m_lastError;

		[SetUp]
		public void SetUpStage()
		{
			// One fixture instance runs every test, so the counters are per test, not per fixture
			m_errors = 0;
			m_lastError = null;

			Container = new InjectorContainer("test");
			Container.Register(new TestService());

			System = new TestSystem();

			Stage = new Stage(Configure(new StageConfig()), Container, "test");
			TkLogger.MessageLogged += OnLog;
		}

		[TearDown]
		public void TearDownStage()
		{
			TkLogger.MessageLogged -= OnLog;
			LogAssert.ignoreFailingMessages = false;

			Stage.Dispose();
			Container.Dispose();

			foreach (var prefab in m_prefabs) {
				if (prefab != null) {
					Object.DestroyImmediate(prefab);
				}
			}

			m_prefabs.Clear();
		}

		protected virtual StageConfig Configure(StageConfig config)
		{
			return config
				.Register<TickBehaviour>()
				.Register<DerivedRegistered>()
				.Register<PlainBehaviour>()
				.RegisterSystem(System)
				.Register<ArgsBehaviour>()
				.Register<InjectedBehaviour>()
				.Register<PhaseLogger>();
		}

		/// <summary>An inactive GameObject with an Actor and the given behaviours, standing in for a prefab asset.</summary>
		protected Actor CreatePrefab(string name, params Type[] behaviours)
		{
			var go = new GameObject(name);
			go.SetActive(false);
			go.AddComponent<Actor>();

			foreach (var type in behaviours) {
				go.AddComponent(type);
			}

			m_prefabs.Add(go);
			return go.GetComponent<Actor>();
		}

		protected T CreatePrefab<T>(string name = null) where T : ActorBehaviour
		{
			return CreatePrefab(name ?? typeof(T).Name, typeof(T)).GetComponent<T>();
		}

		/// <summary>The test expects error logs; they don't fail it.</summary>
		protected void ExpectErrors()
		{
			LogAssert.ignoreFailingMessages = true;
		}

		// Stage logs through TkLogger, so count there rather than on the Unity console
		private void OnLog(TkLogMessage message)
		{
			if (message.Level >= TkLogLevel.Error) {
				m_errors++;
				m_lastError = message.Message;
				if (message.Params != null) {
					foreach (var param in message.Params) {
						m_lastError += " " + param.Value;
					}
				}
			}
		}
	}
}
