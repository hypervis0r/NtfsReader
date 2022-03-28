using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO.Filesystem.Ntfs;
using System.IO;
using System.Diagnostics;
using System;

namespace NtfsReaderTests
{
	[TestClass]
	public class UnitTest1
	{
		[TestMethod]
		public void NtfsReader_ShouldRead()
		{
			DriveInfo driveInfo = new DriveInfo("C:\\");

			var ntfsReader = new NtfsReader(driveInfo, RetrieveMode.Minimal);
		}

		[TestMethod]
		public void NtfsReader_NullDrive()
		{
			Action action = delegate
			{
				var ntfsReader = new NtfsReader(null, RetrieveMode.Minimal);
			};

			Assert.ThrowsException<ArgumentNullException>(action);
		}

		[TestMethod]
		public void NtfsReader_EnumerateNodes()
		{
			DriveInfo driveInfo = new DriveInfo("C:\\");


			var ntfsReader = new NtfsReader(driveInfo, RetrieveMode.Minimal);

			var initialMemory = System.GC.GetTotalMemory(true);

			foreach (var node in ntfsReader.EnumerateNodes(driveInfo.Name))
            {
				Assert.IsNotNull(node);
            }

			var finalMemory = System.GC.GetTotalMemory(true);

			var consumption = finalMemory - initialMemory;

			Trace.WriteLine(
				string.Format(
					"Memory Used: {0}",
					consumption / 1e+6
				)
			);
		}

		[TestMethod]
		public void NtfsReader_GetNodes()
		{
			DriveInfo driveInfo = new DriveInfo("C:\\");


			var ntfsReader = new NtfsReader(driveInfo, RetrieveMode.Minimal);

			var initialMemory = System.GC.GetTotalMemory(true);

			foreach (var node in ntfsReader.GetNodes(driveInfo.Name))
			{
				Assert.IsNotNull(node);
			}

			var finalMemory = System.GC.GetTotalMemory(true);

			var consumption = finalMemory - initialMemory;

			Trace.WriteLine(
				string.Format(
					"Memory Used: {0}",
					consumption / 1e+6
				)
			);
		}
	}
}