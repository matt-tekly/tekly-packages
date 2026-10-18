using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Tekly.DevBoard.Commands;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tekly.DevBoard
{
	[TestFixture]
	public class AsyncCommandTests
	{
		private class Commands
		{
			public readonly List<string> Calls = new();
			public TaskCompletionSource<string> Pending;

			[DevCommand("sync")]
			private string Sync(string value = "ok")
			{
				Calls.Add("sync " + value);
				return value;
			}

			[DevCommand("fail")]
			private void Fail(CommandContext context)
			{
				Calls.Add("fail");
				context.Error("nope");
			}

			[DevCommand("wait")]
			private async Task<string> Wait(CommandContext context)
			{
				context.Log("waiting");
				Pending = new TaskCompletionSource<string>();
				var value = await Pending.Task;
				Calls.Add("wait " + value);
				return value;
			}

			[DevCommand("wait void")]
			private async Task WaitVoid()
			{
				Pending = new TaskCompletionSource<string>();
				await Pending.Task;
				Calls.Add("wait void");
			}

			[DevCommand("cancellable")]
			private async Task Cancellable(CommandContext context)
			{
				Pending = new TaskCompletionSource<string>();
				context.CancellationToken.Register(() => Pending.TrySetCanceled(context.CancellationToken));
				await Pending.Task;
			}

			[DevCommand("throws later")]
			private async Task ThrowsLater()
			{
				await Task.Yield();
				throw new InvalidOperationException("later");
			}
		}

		private CommandRegistry m_registry;
		private Commands m_target;

		[SetUp]
		public void SetUp()
		{
			m_registry = new CommandRegistry();
			m_target = new Commands();
			m_registry.AddFrom(m_target);
		}

		[Test]
		public void SyncCommandsFinishBeforeRunAsyncReturns()
		{
			var task = m_registry.RunAsync("sync hello");

			Assert.IsTrue(task.IsCompleted);
			Assert.IsTrue(task.Result.Succeeded);
			Assert.AreEqual("hello", task.Result.Output.Single().Text);
			Assert.IsFalse(m_registry.Find("sync").IsAsync);
			Assert.IsTrue(m_registry.Find("wait").IsAsync);
		}

		[Test]
		public async Task TaskCommandsWaitAndShowTheirResult()
		{
			var written = new List<string>();
			var task = m_registry.RunAsync("wait", line => written.Add(line.Text));

			// The command's output shows as it's written, before it's done
			Assert.IsFalse(task.IsCompleted);
			CollectionAssert.AreEqual(new[] { "waiting" }, written);

			m_target.Pending.SetResult("done");
			var result = await task;

			Assert.IsTrue(result.Succeeded);
			CollectionAssert.AreEqual(new[] { "waiting", "done" }, result.Output.Select(l => l.Text));
			CollectionAssert.AreEqual(new[] { "waiting", "done" }, written);
		}

		[Test]
		public async Task TaskWithoutResultShowsNothing()
		{
			var task = m_registry.RunAsync("wait void");
			m_target.Pending.SetResult(null);
			var result = await task;

			Assert.IsTrue(result.Succeeded);
			Assert.IsEmpty(result.Output);
			CollectionAssert.AreEqual(new[] { "wait void" }, m_target.Calls);
		}

		[Test]
		public async Task CancellingFailsTheCommand()
		{
			using var cancel = new CancellationTokenSource();
			var task = m_registry.RunAsync("cancellable", cancellationToken: cancel.Token);

			Assert.IsFalse(task.IsCompleted);
			cancel.Cancel();
			var result = await task;

			Assert.IsFalse(result.Succeeded);
			Assert.AreEqual("'cancellable' was cancelled", result.Output.Single().Text);
		}

		[Test]
		public async Task ExceptionsAfterAwaitingFailTheCommand()
		{
			LogAssert.Expect(LogType.Exception, "InvalidOperationException: later");
			var result = await m_registry.RunAsync("throws later");

			Assert.IsFalse(result.Succeeded);
			Assert.AreEqual("InvalidOperationException: later", result.Output.Single().Text);
		}

		[Test]
		public async Task StepsRunInOrderEachAfterTheLast()
		{
			var task = m_registry.RunAsync("sync a; wait; sync b");

			// The second step holds the third
			CollectionAssert.AreEqual(new[] { "sync a" }, m_target.Calls);
			Assert.IsFalse(task.IsCompleted);

			m_target.Pending.SetResult("w");
			var result = await task;

			Assert.IsTrue(result.Succeeded);
			CollectionAssert.AreEqual(new[] { "sync a", "wait w", "sync b" }, m_target.Calls);
			CollectionAssert.AreEqual(new[] { "a", "waiting", "w", "b" }, result.Output.Select(l => l.Text));
		}

		[Test]
		public void StepsStopAtTheFirstFailure()
		{
			var result = m_registry.RunAsync("sync a; fail; sync b; sync c").Result;

			Assert.IsFalse(result.Succeeded);
			CollectionAssert.AreEqual(new[] { "sync a", "fail" }, m_target.Calls);
			CollectionAssert.AreEqual(new[] { "a", "nope", "Stopped: 2 more commands not run" }, result.Output.Select(l => l.Text));
			Assert.IsTrue(result.Output[1].IsError);
			Assert.IsTrue(result.Output[2].IsError);
		}

		[Test]
		public void SemicolonsInQuotesAndBlankStepsAreLeftAlone()
		{
			var result = m_registry.RunAsync("sync \"a; b\"; ; sync c;").Result;

			Assert.IsTrue(result.Succeeded);
			CollectionAssert.AreEqual(new[] { "sync a; b", "sync c" }, m_target.Calls);

			Assert.IsFalse(m_registry.RunAsync("").Result.Succeeded);
			Assert.IsFalse(m_registry.RunAsync("sync; nothing here").Result.Succeeded);
		}

		[Test]
		public void CompletionIsForTheStepAtTheCaret()
		{
			var line = "sync a; wa";
			var completion = m_registry.Complete(line, line.Length);

			var suggestion = completion.Suggestions.Single(s => s.Text == "wait");
			Assert.AreEqual("sync a; wait", suggestion.Apply(line, out var caret));
			Assert.AreEqual(line.Length + 2, caret);

			// Back in the first step, it's that command
			Assert.AreEqual("sync", m_registry.Complete(line, 6).Command.Name);
			Assert.IsNull(completion.Command);
		}
	}
}
