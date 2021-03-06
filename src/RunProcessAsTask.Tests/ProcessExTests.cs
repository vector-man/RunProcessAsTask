﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RunProcessAsTask.Tests
{
    public static class ProcessExTests
    {
        [TestClass]
        public class RunAsyncTests
        {
            [TestMethod]
            public void WhenProcessRunsNormally_ReturnsExpectedResults()
            {
                // Arrange
                const int expectedExitCode = 123;
                const int millisecondsToSleep = 0;
                const int expectedStandardOutputLineCount = 5;
                const int expectedStandardErrorLineCount = 3;
                var pathToConsoleApp = typeof(DummyConsoleApp.Program).Assembly.Location;
                var arguments = String.Join(" ", expectedExitCode, millisecondsToSleep, expectedStandardOutputLineCount, expectedStandardErrorLineCount);

                // Act
                var task = ProcessEx.RunAsync(pathToConsoleApp, arguments);

                // Assert
                Assert.IsNotNull(task);
                var results = task.Result;
                Assert.IsNotNull(results);
                Assert.AreEqual(TaskStatus.RanToCompletion, task.Status);
                Assert.IsNotNull(results.Process);
                Assert.IsTrue(results.Process.HasExited);
                Assert.IsNotNull(results.StandardOutput);
                Assert.IsNotNull(results.StandardError);

                Assert.AreEqual(expectedExitCode, results.Process.ExitCode);
                var standardOutputLines = results.StandardOutput.ToArray();
                var standardErrorLines = results.StandardError.ToArray();
                Assert.AreEqual(expectedStandardOutputLineCount, standardOutputLines.Length);
                Assert.AreEqual(expectedStandardErrorLineCount, standardErrorLines.Length);

                var expectedStandardOutput = new[]
                {
                    "Standard output line #1",
                    "Standard output line #2",
                    "Standard output line #3",
                    "Standard output line #4",
                    "Standard output line #5",
                };
                var expectedStandardError = new[]
                {
                    "Standard error line #1",
                    "Standard error line #2",
                    "Standard error line #3",
                };
                CollectionAssert.AreEqual(expectedStandardOutput, standardOutputLines);
                CollectionAssert.AreEqual(expectedStandardError, standardErrorLines);
            }

            [TestMethod]
            public void WhenProcessTimesOut_TaskIsCanceled()
            {
                // Arrange
                const int expectedExitCode = 123;
                const int millisecondsForTimeout = 3 * 1000;
                const int millisecondsToSleep = 5 * 1000;
                const int expectedStandardOutputLineCount = 5;
                const int expectedStandardErrorLineCount = 3;

                // Act
                var pathToConsoleApp = typeof(DummyConsoleApp.Program).Assembly.Location;
                var arguments = String.Join(" ", expectedExitCode, millisecondsToSleep, expectedStandardOutputLineCount, expectedStandardErrorLineCount);
                var startInfo = new ProcessStartInfo(pathToConsoleApp, arguments);
                var cancellationToken = new CancellationTokenSource(millisecondsForTimeout).Token;
                var task = ProcessEx.RunAsync(startInfo, cancellationToken);

                // Assert
                Assert.IsNotNull(task);
                try
                {
                    var results = task.Result;
                    Assert.Fail("Timeout did not occur");
                }
                catch (AggregateException aggregateException)
                {
                    // expected
                    Assert.AreEqual(1, aggregateException.InnerExceptions.Count);
                    var innerException = aggregateException.InnerExceptions.Single();
                    Assert.IsInstanceOfType(innerException, typeof(OperationCanceledException));
                    var canceledException = innerException as OperationCanceledException;
                    Assert.IsNotNull(canceledException);
                    Assert.IsTrue(cancellationToken.IsCancellationRequested);
                }
                Assert.AreEqual(TaskStatus.Canceled, task.Status);
            }
        }
    }
}
