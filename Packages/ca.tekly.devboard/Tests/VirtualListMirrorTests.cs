using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tekly.DevBoard.Nodes;
using Tekly.DevBoard.Views;

namespace Tekly.DevBoard
{
	/// <summary>
	/// A host's view of a virtual list, against a fake element: only the matches in the element's visible range get
	/// rows built and elements made, searches match items without building them, and revealing a row scrolls to it.
	/// </summary>
	[TestFixture]
	public class VirtualListMirrorTests
	{
		private sealed class FakeHost : IViewHost
		{
			public string Id => "test";
			public bool IsOverlay => false;

			public void Open(string path)
			{
			}
		}

		private sealed class FakeElement : IViewElement
		{
			public readonly Node Node;
			public bool Shown = true;
			public bool Destroyed;

			public FakeElement(Node node)
			{
				Node = node;
			}

			public void SetShown(bool shown) => Shown = shown;
			public void Destroy() => Destroyed = true;
		}

		private sealed class FakeListElement : IVirtualListElement
		{
			/// <summary>
			/// The range the element reports as on screen.
			/// </summary>
			public int First;
			public int End;

			public readonly List<FakeElement> Created = new();
			public readonly List<(int Count, int First, List<FakeElement> Rows)> Shown = new();
			public Binding<string> Search;
			public ListSummary? Summary;
			public int ScrolledToStart;
			public int? ScrolledToRow;

			public bool ChildrenVisible => true;

			public void SetShown(bool shown)
			{
			}

			public void Destroy()
			{
			}

			public void ShowChildren()
			{
			}

			public IViewElement CreateChild(Node child)
			{
				var element = new FakeElement(child);
				Created.Add(element);
				return element;
			}

			public void Order(IReadOnlyList<IViewElement> children)
			{
			}

			public void ShowSearch(Binding<string> query, string placeholder) => Search = query;
			public void ShowSummary(ListSummary summary) => Summary = summary;
			public void ScrollToStart() => ScrolledToStart++;

			public void GetVisibleRange(int count, out int first, out int end)
			{
				first = Math.Clamp(First, 0, count);
				end = Math.Clamp(End, first, count);
			}

			public void ShowRows(int count, int first, IReadOnlyList<IViewElement> rows)
			{
				Shown.Add((count, first, rows.Cast<FakeElement>().ToList()));
			}

			/// <summary>
			/// Puts the row in the middle of a ten row view, as the widget would.
			/// </summary>
			public void ScrollToRow(int index)
			{
				ScrolledToRow = index;
				First = Math.Max(0, index - 5);
				End = First + 10;
			}
		}

		private IDisposable m_board;
		private List<int> m_items;
		private int m_builds;
		private ListNode<int, int> m_list;
		private FakeListElement m_element;
		private VirtualListMirror m_mirror;

		[SetUp]
		public void SetUp()
		{
			// Searches and tabs keep their state in DevBoard.State
			m_board = TestSupport.UseBoard(new DevBoard());
			m_items = Enumerable.Range(0, 100).ToList();
			m_builds = 0;
		}

		[TearDown]
		public void TearDown()
		{
			m_mirror?.Dispose(true);
			m_board.Dispose();
		}

		[Test]
		public void OnlyTheRowsOnScreenAreBuiltAndShown()
		{
			Show(first: 10, end: 20);

			Assert.AreEqual(10, m_builds);
			Assert.AreEqual(10, m_list.Children.Count);

			var shown = m_element.Shown.Single();
			Assert.AreEqual(100, shown.Count);
			Assert.AreEqual(10, shown.First);
			CollectionAssert.AreEqual(Enumerable.Range(10, 10), Items(shown.Rows));
		}

		[Test]
		public void TickingWithoutAChangeShowsNothingAgain()
		{
			Show(first: 10, end: 20);

			m_mirror.Tick();
			m_mirror.Tick();

			Assert.AreEqual(1, m_element.Shown.Count);
		}

		[Test]
		public void ScrollingDropsElementsThatLeftTheScreenAndKeepsTheirRows()
		{
			Show(first: 10, end: 20);
			var leaving = m_element.Created.Take(5).ToList();

			m_element.First = 15;
			m_element.End = 25;
			m_mirror.Tick();

			Assert.IsTrue(leaving.All(element => element.Destroyed), "Rows 10 to 14 scrolled away");
			Assert.AreEqual(15, m_list.Children.Count, "Their rows stay built");
			Assert.AreEqual(15, m_builds, "Only the five new rows were built");

			var shown = m_element.Shown.Last();
			Assert.AreEqual(15, shown.First);
			CollectionAssert.AreEqual(Enumerable.Range(15, 10), Items(shown.Rows));
			Assert.IsTrue(shown.Rows.All(element => !element.Destroyed));
		}

		[Test]
		public void ScrollingBackReusesTheBuiltRowsWithNewElements()
		{
			Show(first: 10, end: 20);
			var row12 = m_list.GetRow(12);

			m_element.First = 50;
			m_element.End = 60;
			m_mirror.Tick();

			m_element.First = 10;
			m_element.End = 20;
			m_mirror.Tick();

			Assert.AreSame(row12, m_list.GetRow(12));
			Assert.AreEqual(20, m_builds, "Rows 10 to 19 and 50 to 59, each built once");
			var shown = m_element.Shown.Last();
			Assert.AreSame(row12, shown.Rows[2].Node);
		}

		[Test]
		public void SearchMatchesItemsWithoutBuildingThem()
		{
			Show(first: 0, end: 10, searchable: true);
			Assert.IsNotNull(m_element.Search, "A searchable list gets a search field");
			Assert.AreEqual(10, m_builds);

			m_element.Search.Write("7");

			// 7, 17, 27, ..., 97 and 70 to 79: 19 items, in item order
			var matches = m_items.Where(item => item.ToString().Contains("7")).ToList();
			Assert.AreEqual(19, matches.Count);

			var shown = m_element.Shown.Last();
			Assert.AreEqual(19, shown.Count, "The scroll range covers every match");
			Assert.AreEqual(0, shown.First);
			CollectionAssert.AreEqual(matches.Take(10), Items(shown.Rows));
			// 7 was already built with the first ten, so nine more
			Assert.AreEqual(19, m_builds, "Only the matches on screen were built");
			Assert.AreEqual(1, m_element.ScrolledToStart);
			Assert.AreEqual(19, m_element.Summary.Value.Shown);
			Assert.IsTrue(m_element.Summary.Value.HasQuery);
			Assert.AreEqual("7", m_element.Search.Read());
		}

		[Test]
		public void TheSearchIsRememberedForTheHost()
		{
			Show(first: 0, end: 10, searchable: true);
			m_element.Search.Write("9");
			m_mirror.Dispose(true);

			// A new mirror for the same host and list, e.g. after the panel's page is rebuilt
			m_element = new FakeListElement { First = 0, End = 10 };
			m_mirror = (VirtualListMirror) NodeMirror.Create(m_list, m_element, new ViewContext(new FakeHost()));

			Assert.AreEqual("9", m_mirror.Query);
			Assert.AreEqual(19, m_element.Shown.Single().Count);
		}

		[Test]
		public void NoMatchesShowsAnEmptyList()
		{
			Show(first: 0, end: 10, searchable: true);

			m_element.Search.Write("zzz");

			var shown = m_element.Shown.Last();
			Assert.AreEqual(0, shown.Count);
			Assert.IsEmpty(shown.Rows);
			Assert.AreEqual(0, m_element.Summary.Value.Shown);
			Assert.IsTrue(m_element.Created.Take(10).All(element => element.Destroyed), "Rows that no longer match lose their elements");
		}

		[Test]
		public void ItemsChangingRematchesAndReplacesRows()
		{
			Show(first: 0, end: 10);
			var before = m_element.Created.ToList();

			// Everything before 50 goes
			m_items.RemoveRange(0, 50);
			TestSupport.PollFresh(m_list);
			m_mirror.Tick();

			Assert.IsTrue(before.All(element => element.Destroyed), "Rows for removed items are gone");
			var shown = m_element.Shown.Last();
			Assert.AreEqual(50, shown.Count);
			CollectionAssert.AreEqual(Enumerable.Range(50, 10), Items(shown.Rows));
		}

		[Test]
		public void RevealingARowOffScreenScrollsToIt()
		{
			Show(first: 0, end: 10);
			var row = m_list.GetRow(60);

			var revealed = m_mirror.Reveal(row);

			Assert.IsNotNull(revealed);
			Assert.AreSame(row, revealed.Node);
			Assert.IsTrue(revealed.IsShown);
			Assert.AreEqual(60, m_element.ScrolledToRow);
			CollectionAssert.Contains(Items(m_element.Shown.Last().Rows), 60);
		}

		[Test]
		public void RevealingARowTheSearchHidesFails()
		{
			Show(first: 0, end: 10, searchable: true);
			m_element.Search.Write("7");
			var row = m_list.GetRow(8);

			Assert.IsNull(m_mirror.Reveal(row));
			Assert.IsNull(m_element.ScrolledToRow);
		}

		[Test]
		public void DisposingLeavesTheRowsAndStopsFollowingTheList()
		{
			Show(first: 0, end: 10);
			var elements = m_element.Created.ToList();

			m_mirror.Dispose(true);
			m_mirror = null;

			// Children's elements go with the list's own element, so they aren't destroyed one by one
			Assert.IsTrue(elements.All(element => !element.Destroyed));
			Assert.AreEqual(10, m_list.Children.Count, "The list's rows aren't the mirror's to remove");

			// No longer listening: a change to the list shows nothing
			m_items.RemoveRange(0, 50);
			TestSupport.PollFresh(m_list);
			Assert.AreEqual(1, m_element.Shown.Count);
		}

		/// <summary>
		/// Builds a virtual list over the items and a mirror for it showing the given range.
		/// </summary>
		private void Show(int first, int end, bool searchable = false)
		{
			TestSupport.BuildScope(scope => {
				m_list = scope.List(() => m_items, item => item, (row, item) => {
					m_builds++;
					row.Label(item.ToString());
				}).WithVirtualRows(20f);

				if (searchable) {
					m_list.WithSearch(item => item.ToString());
				}
			});

			m_element = new FakeListElement { First = first, End = end };
			m_mirror = (VirtualListMirror) NodeMirror.Create(m_list, m_element, new ViewContext(new FakeHost()));
		}

		private static IEnumerable<int> Items(IEnumerable<FakeElement> elements)
		{
			return elements.Select(element => ((ListItem<int>) element.Node).Item);
		}
	}
}
