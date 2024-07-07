using System.Linq;
using DotLiquid;
using Tekly.Injectors;
using Tekly.Logging;
using Tekly.Tinker.Core;
using Tekly.Tinker.Routing;
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
		
		[Get("/inventory"), Description("Get the Inventory"), Command("inventory.json")]
		public ItemInventorySave GetInventory()
		{
			return m_gameWorld.ItemInventory.ToSave();
		}
		
		[Post("/inventory/set"), Description("Set item count"), Command("inventory.set")]
		public string SetInventory(string item, double amount)
		{
			var inventory = m_gameWorld.ItemInventory;
			inventory.SetCount(item, amount);
			return $"Item [{item}] set to [{amount}]";
		}
		
		[Post("/generators/run"), Description("Run a generator"), Command("generator.run")]
		public string RunGenerator(string generator)
		{
			m_gameWorld.GeneratorManager.Run(generator);
			return $"Ran Generator [{generator}]";
		}
		
		[Page("/inventory/card", "tinker_data_card", "Data"), Command("inventory")]
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
		
		[Get("/logs/stats"), Command("logs")]
		public HtmlContent StatsElement()
		{
			return TinkerElements.StatsTopic("logs/stats");
		}
	}
}