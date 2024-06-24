using System.Linq;
using DotLiquid;
using Tekly.Injectors;
using Tekly.Logging;
using Tekly.Tinker.Core;
using TeklySample.Game.Items;
using TeklySample.Game.Worlds;
using UnityEngine;

namespace TeklySample.App
{
	public class LogMessage : Drop
	{
		public string Message { get; set; }
		public LogType Type { get; set; }
	}

	[Route("/game"), Description("Game")]
	public class GameRoutes
	{
		[Inject] private GameWorld m_gameWorld;
		
		[Post("/log"), Description("Test Log")]
		public LogMessage LogMessage([LargeText] string message, LogType logType = LogType.Log)
		{
			Debug.LogFormat(logType, LogOption.None, null, message);

			return new LogMessage {
				Message = message,
				Type = logType
			};
		}
		
		[Get("/inventory"), Description("Get the Inventory")]
		public ItemInventorySave GetInventory()
		{
			return m_gameWorld.ItemInventory.ToSave();
		}
		
		[Post("/inventory/set"), Description("Set item count")]
		public ItemInventorySave GetInventory(string item, double amount)
		{
			var inventory = m_gameWorld.ItemInventory;
			inventory.SetCount(item, amount);
			return inventory.ToSave();
		}
		
		[Post("/generators/run"), Description("Run a generator")]
		public string RunGenerator(string generator)
		{
			m_gameWorld.GeneratorManager.Run(generator);
			return "okay?";
		}
		
		[Page("/inventory/card", "tinker_data_list", "Data")]
		public DataList GetInventoryCard()
		{
			var dataList = new DataList("Inventory");

			foreach (var inventoryItem in m_gameWorld.ItemInventory.Items.OrderBy(x => x.ItemId)) {
				dataList.Add(inventoryItem.ItemId, inventoryItem.Count);
			}

			return dataList;
		}

		[Page("/logs/all", "logs")]
		public LogMessage LogMessage()
		{
			return new LogMessage {
				Message = "Test Log",
				Type = LogType.Log
			};
		}

		[Page("/logs/stats", "tinker_stats_card", "Stats")]
		public DataList Stats()
		{
			return new DataList("Logs")
				.Add("Debug", TkLogger.Stats.Debug)
				.Add("Info", TkLogger.Stats.Info)
				.Add("Warning", TkLogger.Stats.Warning, "yellow")
				.Add("Error", TkLogger.Stats.Error, "red")
				.Add("Exception", TkLogger.Stats.Exception, "red");
		}
	}
}