using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Tekly.DevBoard.Nodes;
using Tekly.DevBoard.Pages;

namespace Tekly.DevBoard
{
	/// <summary>
	/// How the page tree grows and shrinks with segments, and how changes made from inside its own events are held
	/// back until the current change is done.
	/// </summary>
	[TestFixture]
	public class PageTreeTests
	{
		private PageTree m_tree;
		private List<string> m_events;

		[SetUp]
		public void SetUp()
		{
			m_tree = new PageTree();
			m_events = new List<string>();

			m_tree.PageAdded += page => m_events.Add($"+page {page.Path}");
			m_tree.PageRemoved += page => m_events.Add($"-page {page.Path}");
			m_tree.SegmentAdded += segment => m_events.Add($"+segment {segment.Path}");
			m_tree.SegmentRemoved += (segment, page) => m_events.Add($"-segment {page.Path}");
			m_tree.ChildrenChanged += page => m_events.Add($"children {Name(page)}");
		}

		[Test]
		public void AddingASegmentCreatesItsPageAndMissingParents()
		{
			var segment = m_tree.Add("Game/Economy", Nothing);

			var economy = m_tree.Find("Game/Economy");
			var game = m_tree.Find("Game");

			Assert.AreSame(economy, segment.Node);
			Assert.AreEqual("Economy", economy.Title);
			Assert.AreSame(game, economy.Parent);
			Assert.AreEqual(0, game.Segments.Count, "A folder page has no segments of its own");
			CollectionAssert.AreEqual(new[] { game }, m_tree.Root.Children);
			CollectionAssert.AreEqual(new[] { segment }, economy.Segments);

			CollectionAssert.AreEqual(new[] {
				"+page Game", "children root",
				"+page Game/Economy", "children Game",
				"+segment Game/Economy"
			}, m_events);
		}

		[Test]
		public void DisposingTheLastSegmentPrunesEmptyPagesUpwards()
		{
			var segment = m_tree.Add("Game/Economy", Nothing);
			var economy = m_tree.Find("Game/Economy");
			var game = m_tree.Find("Game");
			m_events.Clear();

			segment.Dispose();

			Assert.IsTrue(segment.IsDisposed);
			Assert.IsNull(segment.Node);
			Assert.IsTrue(economy.IsRemoved);
			Assert.IsTrue(game.IsRemoved);
			Assert.IsNull(m_tree.Find("Game/Economy"));
			Assert.IsNull(m_tree.Find("Game"));
			Assert.AreEqual(0, m_tree.Root.Children.Count);

			CollectionAssert.AreEqual(new[] {
				"-segment Game/Economy",
				"-page Game/Economy", "children Game",
				"-page Game", "children root"
			}, m_events);
		}

		[Test]
		public void PruningStopsAtAPageThatStillHasSomething()
		{
			var a = m_tree.Add("Game/A", Nothing);
			m_tree.Add("Game/B", Nothing);

			a.Dispose();

			Assert.IsNull(m_tree.Find("Game/A"));
			Assert.IsNotNull(m_tree.Find("Game"));
			Assert.IsNotNull(m_tree.Find("Game/B"));
		}

		[Test]
		public void DisposingTwiceDoesNothingMore()
		{
			var segment = m_tree.Add("Game", Nothing);
			segment.Dispose();
			m_events.Clear();

			segment.Dispose();

			Assert.IsEmpty(m_events);
		}

		[Test]
		public void ChangesMadeInsideAnEventWaitForTheCurrentChange()
		{
			PageNode otherDuringEvent = null;
			PageSegment added = null;

			m_tree.SegmentAdded += segment => {
				if (added == null) {
					added = m_tree.Add("Other", Nothing);
					otherDuringEvent = m_tree.Find("Other");
				}
			};

			m_tree.Add("Game", Nothing);

			Assert.IsNull(otherDuringEvent, "Not attached while the first add's events are still running");
			Assert.IsNotNull(m_tree.Find("Other"), "Attached once the first add finished");
			Assert.AreSame(m_tree.Find("Other"), added.Node);
			CollectionAssert.AreEqual(new[] { "Game", "Other" }, m_tree.Root.Children.Select(page => page.Title));
		}

		[Test]
		public void ASegmentDisposedBeforeItsQueuedAddRunsIsNeverAttached()
		{
			m_tree.SegmentAdded += segment => {
				if (segment.Path == "Game") {
					var other = m_tree.Add("Other", Nothing);
					other.Dispose();
				}
			};

			m_tree.Add("Game", Nothing);

			Assert.IsNull(m_tree.Find("Other"));
			Assert.IsFalse(m_events.Contains("+page Other"));
		}

		[Test]
		public void AHandlerThatThrowsDoesNotStopTheOthers()
		{
			var heard = false;
			m_tree.SegmentAdded += _ => throw new System.InvalidOperationException("boom");
			m_tree.SegmentAdded += _ => heard = true;

			UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Exception, new System.Text.RegularExpressions.Regex("boom"));
			var segment = m_tree.Add("Game", Nothing);

			Assert.IsTrue(heard);
			Assert.IsNotNull(segment.Node);
		}

		[Test]
		public void SegmentsSortByOrderThenByWhenTheyWereAdded()
		{
			var late = m_tree.Add("Game", Nothing, order: 1);
			var first = m_tree.Add("Game", Nothing, order: 0);
			var alsoLate = m_tree.Add("Game", Nothing, order: 1);

			CollectionAssert.AreEqual(new[] { first, late, alsoLate }, m_tree.Find("Game").Segments);
		}

		[Test]
		public void PagesSortByTheirLowestOrderAndResortWhenItChanges()
		{
			m_tree.Add("Zoo", Nothing, order: 2);
			m_tree.Add("Apples", Nothing, order: 5);
			CollectionAssert.AreEqual(new[] { "Zoo", "Apples" }, Titles(m_tree.Root.Children));

			// A folder takes its children's lowest order
			m_tree.Add("Mid/Thing", Nothing, order: 3);
			CollectionAssert.AreEqual(new[] { "Zoo", "Mid", "Apples" }, Titles(m_tree.Root.Children));
			m_events.Clear();

			// A lower order segment moves its page, and tells the parent its children changed
			var early = m_tree.Add("Apples", Nothing, order: 0);
			CollectionAssert.AreEqual(new[] { "Apples", "Zoo", "Mid" }, Titles(m_tree.Root.Children));
			Assert.Contains("children root", m_events);

			// Removing it moves the page back
			early.Dispose();
			CollectionAssert.AreEqual(new[] { "Zoo", "Mid", "Apples" }, Titles(m_tree.Root.Children));
		}

		[Test]
		public void EqualOrdersSortByTitle()
		{
			m_tree.Add("b", Nothing);
			m_tree.Add("A", Nothing);
			m_tree.Add("c", Nothing);

			CollectionAssert.AreEqual(new[] { "A", "b", "c" }, Titles(m_tree.Root.Children));
		}

		[Test]
		public void FindNearestFallsBackUpThePath()
		{
			m_tree.Add("Game/Economy", Nothing);

			Assert.AreEqual("Game/Economy", m_tree.FindNearest("Game/Economy/Gold").Path);
			Assert.AreEqual("Game", m_tree.FindNearest("Game/Missing").Path);
			Assert.AreSame(m_tree.Root, m_tree.FindNearest("Nope/Nope"));
		}

		[Test]
		public void PathsAreNormalized()
		{
			Assert.AreEqual("Game/Economy", PageTree.NormalizePath(" Game//Economy/ "));
			Assert.AreEqual(string.Empty, PageTree.NormalizePath(null));
			Assert.AreEqual(string.Empty, PageTree.NormalizePath(" / "));

			var segment = m_tree.Add(" Game//Economy/ ", Nothing);
			Assert.AreEqual("Game/Economy", segment.Path);
			Assert.AreSame(segment.Node, m_tree.Find("Game/Economy"));
		}

		[Test]
		public void ContentIsBuiltOnFirstAskAndDisposedAfterRemoval()
		{
			var builds = 0;
			var disposed = false;

			var segment = m_tree.Add("Game", page => {
				builds++;
				page.Label("hello");
				page.OnDispose(() => disposed = true);
			});

			Assert.AreEqual(0, builds, "Not built until something asks");

			var root = segment.Root;
			Assert.AreSame(root, segment.Root, "Built once");
			Assert.AreEqual(1, builds);
			Assert.AreEqual("game/segment", root.Path);
			Assert.AreEqual("game/segment/hello", root.Children[0].Path);

			var disposedWhenRemoved = false;
			m_tree.SegmentRemoved += (_, _) => disposedWhenRemoved = disposed;

			segment.Dispose();

			Assert.IsFalse(disposedWhenRemoved, "Panels hear about the removal before the content's OnDispose runs");
			Assert.IsTrue(disposed);
			Assert.IsTrue(root.IsDisposed);
			Assert.IsNull(segment.Root);
		}

		[Test]
		public void ABuilderThatThrowsShowsTheErrorInstead()
		{
			var segment = m_tree.Add("Game", _ => throw new System.InvalidOperationException("broken builder"));

			UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Exception, new System.Text.RegularExpressions.Regex("broken builder"));
			var root = segment.Root;

			Assert.AreEqual(1, root.Children.Count);
			var error = (LabelNode) root.Children[0];
			Assert.AreEqual(LabelKind.Error, error.Kind);
			StringAssert.Contains("broken builder", error.Text);
			Assert.IsTrue(root.IsFrozen);
		}

		private static void Nothing(Segment page)
		{
		}

		private static string Name(PageNode page)
		{
			return page.IsRoot ? "root" : page.Path;
		}

		private static IEnumerable<string> Titles(IEnumerable<PageNode> pages)
		{
			return pages.Select(page => page.Title);
		}
	}
}
