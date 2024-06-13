using System;
using Tekly.Simulant.Core;
using Tekly.Simulant.Templates;

namespace TeklySample.Samples.Town
{
	public struct GeneratorData
	{
		public float Timer;
	}
	
	[Serializable]
	public struct GeneratorConfig
	{
		public string Resource;
		public int Count;
		public float Duration;
	}
	
	public class GeneratorDefinition : EntityTemplate
	{
		public GeneratorConfig Config = new GeneratorConfig {
			Resource = "lemon",
			Count = 1,
			Duration = 1,
		};
		
		protected override void OnPopulate(World world, int entity)
		{
			world.Add(entity, ref Config);
			world.Add<GeneratorData>(entity);
		}
	}

	public class GeneratorSystem : TownSystem
	{
		private readonly DataPool<GeneratorConfig> m_configs;
		private readonly DataPool<GeneratorData> m_datas;
		private readonly Query m_generators;
		
		public GeneratorSystem(World world) : base(world)
		{
			m_configs = world.GetPool<GeneratorConfig>();
			m_datas = world.GetPool<GeneratorData>();

			m_generators = world.Query<GeneratorConfig, GeneratorData>();
		}
		
		public override void Tick(float deltaTime)
		{
			foreach (var generator in m_generators) {
				ref var data = ref m_datas.Get(generator);
				ref var config = ref m_configs.Get(generator);

				data.Timer += deltaTime;
				
				if (data.Timer >= config.Duration) {
					data.Timer -= config.Duration;
					
					ref var add = ref m_world.Add<AddToInventory>(m_world.Create());
					add.Resource = config.Resource;
					add.Count = config.Count;
				}
			}
		}
	}
}