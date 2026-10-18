using NUnit.Framework;
using Tekly.DevBoard.Nodes;
using Tekly.DevBoard.Pages;

namespace Tekly.DevBoard
{
	/// <summary>
	/// What is and isn't a level in node paths: anonymous layout is looked through, so moving a control between
	/// columns, rows and cards keeps its path, while labelled or identified containers, foldouts, tabs and lists are
	/// levels of their own.
	/// </summary>
	[TestFixture]
	public class NodePathTests
	{
		private PageTree m_tree;

		[SetUp]
		public void SetUp()
		{
			m_tree = new PageTree();
		}

		[Test]
		public void AnonymousLayoutAddsNoLevel()
		{
			var root = Build(page => {
				var row = page.Row();
				var card = row.Card();
				var column = card.Column();
				column.Button("Give Gold", null);
				page.Scope(scope => scope.Toggle("Muted", () => false, _ => { }));
			});

			var row = (ContainerNode) root.Children[0];
			var button = Find(root, "Give Gold");
			var toggle = Find(root, "Muted");

			Assert.AreEqual("game/segment/give-gold", button.Path);
			Assert.AreEqual("game/segment/muted", toggle.Path);
			Assert.IsNull(row.Id, "Anonymous layout has no id of its own");
			Assert.AreEqual(root.Path, row.Path, "Its path is the level it sits in");
		}

		[Test]
		public void IdsAreUniqueAcrossAnonymousLayoutAtTheSameLevel()
		{
			var root = Build(page => {
				page.Row().Button("Reset", null);
				page.Row().Button("Reset", null);
				page.Column().Card().Button("Reset", null);
				page.Button("Reset", null);
			});

			Assert.AreEqual("game/segment/reset", Find(root, "Reset", 0).Path);
			Assert.AreEqual("game/segment/reset-2", Find(root, "Reset", 1).Path);
			Assert.AreEqual("game/segment/reset-3", Find(root, "Reset", 2).Path);
			Assert.AreEqual("game/segment/reset-4", Find(root, "Reset", 3).Path);
		}

		[Test]
		public void LabelledOrIdentifiedContainersAreLevels()
		{
			var root = Build(page => {
				page.Column().WithId("left").Button("Reset", null);
				page.Add(new ContainerNode(ContainerKind.Card, "Cheats")).Button("Reset", null);
				// A container setter keeps the foldout's type, so its own settings can follow
				page.Foldout("More").WithPadding(4).WithStartExpanded().Button("Reset", null);
				page.Tabs().Tab("Audio").Button("Reset", null);
			});

			Assert.AreEqual("game/segment/left/reset", Find(root, "Reset", 0).Path);
			Assert.AreEqual("game/segment/cheats/reset", Find(root, "Reset", 1).Path);
			Assert.AreEqual("game/segment/more/reset", Find(root, "Reset", 2).Path);
			Assert.AreEqual("game/segment/tabs/audio/reset", Find(root, "Reset", 3).Path);
		}

		[Test]
		public void ListRowsAreLevelsUnderTheList()
		{
			var items = new[] { "Alice", "Bob" };

			var root = Build(page => {
				page.Row().List(() => items, name => name, (row, name) => {
					row.Row().Button("Kick", null);
				});
			});

			Assert.AreEqual("game/segment/list/alice/kick", Find(root, "Kick", 0).Path);
			Assert.AreEqual("game/segment/list/bob/kick", Find(root, "Kick", 1).Path);
		}

		[Test]
		public void IdsFreedByARemovedScopeCanBeTakenAgain()
		{
			ScopeNode scope = null;
			var count = 1;

			var root = Build(page => {
				scope = page.Scope(s => {
					for (var i = 0; i < count; i++) {
						s.Row().Button("Reset", null);
					}
				});
				page.Button("Reset", null);
			});

			// The page's own children take ids first, then the scope builds and its button takes the next
			Assert.AreEqual("game/segment/reset-2", Find(root, "Reset", 0).Path);
			Assert.AreEqual("game/segment/reset", Find(root, "Reset", 1).Path);

			// Rebuilding frees the scope's ids, so the first row takes the same id again and the second the next
			// free one, leaving the page's own button where it was
			count = 2;
			scope.RebuildNow();

			Assert.AreEqual("game/segment/reset-2", Find(root, "Reset", 0).Path);
			Assert.AreEqual("game/segment/reset-3", Find(root, "Reset", 1).Path);
			Assert.AreEqual("game/segment/reset", Find(root, "Reset", 2).Path);
		}

		[Test]
		public void NodeAccessFindsNodesThroughAnonymousLayout()
		{
			Build(page => page.Row().Card().Slider("Volume", 0f, 1f, () => 0f, _ => { }));
			var access = new Access.NodeAccess(m_tree);

			Assert.IsInstanceOf<SliderNode>(access.Find("game/segment/volume"));
			Assert.IsNull(access.Find("game/segment/row/volume"));
		}

		private Segment Build(System.Action<Segment> build)
		{
			return m_tree.Add("Game", build).Root;
		}

		/// <summary>
		/// The occurrence'th node labelled label, depth first.
		/// </summary>
		private static Node Find(ContainerNode container, string label, int occurrence = 0)
		{
			var seen = 0;
			return FindUnder(container, label, ref seen, occurrence);
		}

		private static Node FindUnder(ContainerNode container, string label, ref int seen, int occurrence)
		{
			foreach (var child in container.Children) {
				if (child.Label == label && !(child is ContainerNode)) {
					if (seen++ == occurrence) {
						return child;
					}
				}

				if (child is ContainerNode nested) {
					var found = FindUnder(nested, label, ref seen, occurrence);

					if (found != null) {
						return found;
					}
				}
			}

			return null;
		}
	}
}
