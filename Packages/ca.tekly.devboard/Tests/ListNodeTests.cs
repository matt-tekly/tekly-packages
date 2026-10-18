using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tekly.DevBoard.Nodes;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tekly.DevBoard
{
	/// <summary>
	/// How a list keeps its rows in step with its data: rows are matched by key, kept when their key stays, rebuilt
	/// when their item changes, and removed when their key goes.
	/// </summary>
	[TestFixture]
	public class ListNodeTests
	{
		private sealed class Player
		{
			public string Id;
			public int Score;

			public Player(string id, int score)
			{
				Id = id;
				Score = score;
			}
		}

		private List<string> m_names;
		private List<Player> m_players;
		private int m_builds;

		[SetUp]
		public void SetUp()
		{
			m_names = new List<string> { "Alice", "Bob", "Carol" };
			m_players = new List<Player> { new("a", 1), new("b", 2) };
			m_builds = 0;
		}

		[Test]
		public void BuildsARowPerItemInOrder()
		{
			var list = Names();

			Assert.AreEqual(3, list.Children.Count);
			CollectionAssert.AreEqual(m_names, Items(list));
			CollectionAssert.AreEqual(new[] { "alice", "bob", "carol" }, list.Children.Select(row => row.Id));
			Assert.AreEqual(3, m_builds);
		}

		[Test]
		public void RowsFollowTheItemsByKey()
		{
			var list = Names();
			var bob = Row(list, "Bob");
			var carol = Row(list, "Carol");

			// Alice goes, Dave arrives, and the rest swap places
			m_names.Clear();
			m_names.AddRange(new[] { "Carol", "Dave", "Bob" });
			TestSupport.PollFresh(list);

			CollectionAssert.AreEqual(m_names, Items(list));
			Assert.AreSame(bob, Row(list, "Bob"), "A row whose key stays keeps its node");
			Assert.AreSame(carol, Row(list, "Carol"));
			Assert.AreEqual(4, m_builds, "Only the new row is built");
			CollectionAssert.AreEqual(new[] { 0, 1, 2 }, list.Children.Cast<ListItem<string>>().Select(row => row.ListIndex));
		}

		[Test]
		public void RemovedRowsAreDisposed()
		{
			var list = Names();
			var alice = Row(list, "Alice");
			var disposed = false;
			alice.OnDispose(() => disposed = true);

			m_names.Remove("Alice");
			TestSupport.PollFresh(list);

			Assert.IsTrue(disposed);
			Assert.IsTrue(alice.IsDisposed);
			Assert.AreEqual(2, list.Children.Count);
		}

		[Test]
		public void RowIsRebuiltWhenItsItemChangesUnderTheSameKey()
		{
			var list = Players();
			var row = Row(list, "a");

			// The row builder ran once per player so far
			Assert.AreEqual(2, m_builds);

			// A new object for the same key: the row is kept but built again with the new item
			m_players[0] = new Player("a", 10);
			TestSupport.PollFresh(list);

			Assert.AreSame(row, Row(list, "a"));
			Assert.AreEqual(10, row.Item.Score);
			Assert.AreEqual(3, m_builds);
			Assert.AreEqual("a: 10", ((LabelNode) row.Children[0]).Text);
		}

		[Test]
		public void SameItemIsNotRebuilt()
		{
			var list = Players();

			// Mutating the object in place isn't a change the list can see
			m_players[0].Score = 99;
			TestSupport.PollFresh(list);

			Assert.AreEqual(2, m_builds);
		}

		[Test]
		public void DuplicateKeysKeepTheFirstAndWarnOnce()
		{
			m_names.Add("Bob");

			LogAssert.Expect(LogType.Warning, "[DevBoard] A list has more than one item with the key 'Bob'. Only the first is shown.");
			var list = Names();

			Assert.AreEqual(3, list.Children.Count);

			// Still duplicated on the next sync, but not warned about again
			TestSupport.PollFresh(list);
			LogAssert.NoUnexpectedReceived();
		}

		[Test]
		public void ChildrenChangedIsRaisedOncePerSyncAndOnlyOnAChange()
		{
			var list = Names();
			var raised = 0;
			list.ChildrenChanged += _ => raised++;

			TestSupport.PollFresh(list);
			Assert.AreEqual(0, raised, "Nothing changed");

			// One add, one remove and a reorder in one sync
			m_names.Clear();
			m_names.AddRange(new[] { "Carol", "Alice", "Dave" });
			TestSupport.PollFresh(list);

			Assert.AreEqual(1, raised);
		}

		[Test]
		public void VersionSkipsTheDataWhileItStaysTheSame()
		{
			var version = 1;
			var reads = 0;
			ListNode<string, string> list = null;

			TestSupport.BuildScope(scope => {
				list = scope.List(() => {
					reads++;
					return m_names;
				}, name => name, (row, name) => row.Label(name)).WithVersion(() => version);
			});

			Assert.AreEqual(1, reads);

			TestSupport.PollFresh(list);
			Assert.AreEqual(1, reads, "Same version, data not read");

			version++;
			m_names.Add("Dave");
			TestSupport.PollFresh(list);

			Assert.AreEqual(2, reads);
			Assert.AreEqual(4, list.Children.Count);
		}

		[Test]
		public void VirtualListBuildsRowsOnlyWhenAsked()
		{
			var list = Names(virtualRows: true);

			Assert.AreEqual(3, list.ItemCount);
			Assert.AreEqual(0, list.Children.Count, "No rows built until a panel shows them");
			Assert.AreEqual(0, m_builds);

			var carol = (ListItem<string>) list.GetRow(2);
			var alice = (ListItem<string>) list.GetRow(0);

			Assert.AreEqual("Carol", carol.Item);
			Assert.AreEqual(2, m_builds);
			CollectionAssert.AreEqual(new Node[] { alice, carol }, list.Children, "Built rows stay in item order");
			Assert.AreSame(carol, list.GetRow(2), "Asking again returns the same row");
			Assert.AreEqual(2, list.IndexOfRow(carol));
			Assert.AreEqual(-1, list.IndexOfRow(new LabelNode("not a row")));
		}

		[Test]
		public void VirtualListKeepsBuiltRowsInStepWithTheData()
		{
			var list = Names(virtualRows: true);
			var carol = list.GetRow(2);

			m_names.Insert(0, "Zed");
			m_names.Remove("Alice");
			TestSupport.PollFresh(list);

			Assert.AreEqual(3, list.ItemCount);
			Assert.AreEqual(1, list.Children.Count, "Only the row that was built exists");
			Assert.AreSame(carol, list.GetRow(2));
			Assert.AreEqual(2, list.IndexOfRow(carol));
		}

		[Test]
		public void MatchesSearchesTheItemText()
		{
			ListNode<string, string> list = null;

			TestSupport.BuildScope(scope => {
				list = scope.List(() => m_names, name => name, (row, name) => row.Label(name)).WithSearch(name => name);
			});

			var query = new Common.Search.SearchQuery("bo");

			Assert.IsTrue(list.Matches(Row(list, "Bob"), query));
			Assert.IsFalse(list.Matches(Row(list, "Alice"), query));
			Assert.IsFalse(list.Matches(new LabelNode("Bob"), query), "Only this list's rows match");
		}

		private ListNode<string, string> Names(bool virtualRows = false)
		{
			ListNode<string, string> list = null;

			TestSupport.BuildScope(scope => {
				list = scope.List(() => m_names, name => name, (row, name) => {
					m_builds++;
					row.Label(name);
				});

				if (virtualRows) {
					list.WithVirtualRows(20f);
				}
			});

			return list;
		}

		private ListNode<Player, string> Players()
		{
			ListNode<Player, string> list = null;

			TestSupport.BuildScope(scope => {
				list = scope.List(() => m_players, player => player.Id, (row, player) => {
					m_builds++;
					row.Label($"{player.Id}: {player.Score}");
				});
			});

			return list;
		}

		private static IEnumerable<string> Items(ListNode<string, string> list)
		{
			return list.Children.Cast<ListItem<string>>().Select(row => row.Item);
		}

		private static ListItem<T> Row<T>(ListNode<T, string> list, string key)
		{
			return list.Children.Cast<ListItem<T>>().First(row => row.KeyText == key);
		}
	}
}
