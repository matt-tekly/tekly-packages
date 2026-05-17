using NUnit.Framework;
using AssertionException = UnityEngine.Assertions.AssertionException;

namespace Tekly.Common.Utils
{
    [TestFixture]
    public class LatchTests
    {
       [Test]
       public void StartsUnheld()
       {
          var latch = new Latch();
          Assert.IsFalse(latch.IsHeld.Value);
       }

       [Test]
       public void HoldSetsIsHeld()
       {
          var latch = new Latch();
          var holder = new object();

          latch.Hold(holder);

          Assert.IsTrue(latch.IsHeld.Value);
       }

       [Test]
       public void ReleaseClearsIsHeldWhenLastHolderRemoved()
       {
          var latch = new Latch();
          var holder = new object();

          latch.Hold(holder);
          latch.Release(holder);

          Assert.IsFalse(latch.IsHeld.Value);
       }

       [Test]
       public void MultipleHoldersKeepLatchHeldUntilAllReleased()
       {
          var latch = new Latch();
          var a = new object();
          var b = new object();

          latch.Hold(a);
          latch.Hold(b);

          Assert.IsTrue(latch.IsHeld.Value);

          latch.Release(a);
          Assert.IsTrue(latch.IsHeld.Value);

          latch.Release(b);
          Assert.IsFalse(latch.IsHeld.Value);
       }

       [Test]
       public void HoldScopeReleasesOnDispose()
       {
          var latch = new Latch();
          var holder = new object();

          var scope = latch.HoldScope(holder);
          Assert.IsTrue(latch.IsHeld.Value);

          scope.Dispose();
          Assert.IsFalse(latch.IsHeld.Value);
       }

       [Test]
       public void HoldScopeDisposeIsIdempotent()
       {
          var latch = new Latch();
          var holder = new object();

          var scope = latch.HoldScope(holder);
          scope.Dispose();
          scope.Dispose();
          Assert.IsFalse(latch.IsHeld.Value);
       }

       [Test]
       public void ValidatesInput()
       {
          var latch = new Latch();

          Assert.Throws<AssertionException>(() => latch.Hold(null));
          Assert.Throws<AssertionException>(() => latch.HoldScope(null));
          Assert.Throws<AssertionException>(() => latch.Release(new object()));
          Assert.Throws<AssertionException>(() => new Latch(null));
       }

       [Test]
       public void ChildFollowsParent()
       {
          var parent = new Latch();
          var child = new Latch(parent);

          Assert.IsFalse(child.IsHeld.Value);

          var parentHolder = new object();
          parent.Hold(parentHolder);
          Assert.IsTrue(child.IsHeld.Value);

          parent.Release(parentHolder);
          Assert.IsFalse(child.IsHeld.Value);
       }

       [Test]
       public void ChildCanBeHeldIndependentlyOfParent()
       {
          var parent = new Latch();
          var child = new Latch(parent);
          var childHolder = new object();

          child.Hold(childHolder);
          Assert.IsTrue(child.IsHeld.Value);
          Assert.IsFalse(parent.IsHeld.Value);

          child.Release(childHolder);
          Assert.IsFalse(child.IsHeld.Value);
       }

       [Test]
       public void ChildStaysHeldWhileEitherParentOrOwnHolderActive()
       {
          var parent = new Latch();
          var child = new Latch(parent);
          var parentHolder = new object();
          var childHolder = new object();

          parent.Hold(parentHolder);
          child.Hold(childHolder);
          Assert.IsTrue(child.IsHeld.Value);

          parent.Release(parentHolder);
          Assert.IsTrue(child.IsHeld.Value);

          child.Release(childHolder);
          Assert.IsFalse(child.IsHeld.Value);
       }

       [Test]
       public void DisposingChildReleasesParentHold()
       {
          var parent = new Latch();
          var child = new Latch(parent);
          var parentHolder = new object();

          parent.Hold(parentHolder);
          Assert.IsTrue(child.IsHeld.Value);

          child.Dispose();
          Assert.IsFalse(child.IsHeld.Value);
       }
    }
}