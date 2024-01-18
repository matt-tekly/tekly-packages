using System;
using System.Collections.Generic;
using Tekly.Injectors;
using Tekly.Simulant.Core;
using Tekly.Simulant.Extensions.Systems;
using Tekly.Simulant.Systems;
using UnityEngine;

namespace TeklySample.Samples.CubeMovement
{
	[Serializable]
	public struct BasicRendererData : ITransient
	{
		public Mesh Mesh;
		public Material Material;
		
		public BasicRendererData(Mesh mesh, Material material)
		{
			Mesh = mesh;
			Material = material;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Mesh.GetHashCode(), Material.GetHashCode());
		}
	}
	
	public struct RendererRef : ITransient
	{
		public InstancedRenderer Renderer;
	}
	
	public class MeshSystem : ITickSystem, IQueryListener
	{
		[Inject] private DataPool<BasicRendererData> m_rendererData;
		[Inject] private DataPool<TransformData> m_transformData;
		[Inject] private DataPool<RendererRef> m_rendererRefs;
		[Inject] private World m_world;
		[Inject] private Camera m_camera;

		private Query m_meshes;
		
		private Dictionary<BasicRendererData, InstancedRenderer> m_renderers = new Dictionary<BasicRendererData, InstancedRenderer>();
		
		[Inject]
		public void Init()
		{
			m_meshes = m_world.Query<BasicRendererData, TransformData>();

			foreach (var entity in m_meshes) {
				EntityAdded(entity, m_meshes);
			}
		}

		public void Tick(float deltaTime)
		{
			foreach (var entity in m_meshes) {
				ref var transformData = ref m_transformData.Get(entity);
				ref var rendererRef = ref m_rendererRefs.Get(entity);
				
				rendererRef.Renderer.Add(ref transformData);
			}

			foreach (var renderer in m_renderers.Values) {
				renderer.Flush();
			}
		}

		public void EntityAdded(int entity, Query query)
		{
			ref var meshData = ref m_rendererData.Get(entity);
				
			if (!m_renderers.TryGetValue(meshData, out var renderer)) {
				renderer = new InstancedRenderer(meshData.Material, meshData.Mesh);
				m_renderers[meshData] = renderer;
			}
				
			m_rendererRefs.Add(entity, new RendererRef {
				Renderer = renderer
			});
		}

		public void EntityRemoved(int entity, Query query)
		{
			m_rendererRefs.Delete(entity);
		}
	}
	
	public class InstancedRenderer
	{
		private readonly Material m_material;
		private readonly Mesh m_mesh;

		private readonly RenderParams m_renderParams;
		
		private readonly Matrix4x4[] m_matrices = new Matrix4x4[1023];
		private int m_count;

		public InstancedRenderer(Material material, Mesh mesh)
		{
			m_renderParams = new RenderParams(material);
			m_mesh = mesh;
		}
		
		public void Add(ref TransformData transformData)
		{
			m_matrices[m_count] = Matrix4x4.TRS(transformData.Position, transformData.Rotation, transformData.Scale);
			m_count++;

			if (m_count >= 1023) {
				Flush();
			}
		}

		public void Flush()
		{
			if (m_count == 0) {
				return;
			}
			
			Graphics.RenderMeshInstanced(in m_renderParams, m_mesh, 0, m_matrices, m_count );
			
			m_count = 0;
		}
	}
}