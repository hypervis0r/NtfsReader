﻿/*
	The NtfsReader library.

	Copyright (C) 2008 Danny Couture

	This library is free software; you can redistribute it and/or
	modify it under the terms of the GNU Lesser General Public
	License as published by the Free Software Foundation; either
	version 2.1 of the License, or (at your option) any later version.

	This library is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
	Lesser General Public License for more details.

	You should have received a copy of the GNU Lesser General Public
	License along with this library; if not, write to the Free Software
	Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
  
	For the full text of the license see the "License.txt" file.

	This library is based on the work of Jeroen Kessels, Author of JkDefrag.
	http://www.kessels.com/Jkdefrag/
	
	Special thanks goes to him.
  
	Danny Couture
	Software Architect
*/
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;

namespace System.IO.Filesystem.Ntfs
{
	/// <summary>
	/// Ntfs metadata reader.
	/// 
	/// This class is used to get files & directories information of an NTFS volume.
	/// This is a lot faster than using conventional directory browsing method
	/// particularly when browsing really big directories.
	/// </summary>
	/// <remarks>Admnistrator rights are required in order to use this method.</remarks>
	public partial class NtfsReader
	{
		/// <summary>
		/// NtfsReader constructor.
		/// </summary>
		/// <param name="driveInfo">The drive you want to read metadata from.</param>
		/// <param name="include">Information to retrieve from each node while scanning the disk</param>
		/// <remarks>Streams & Fragments are expensive to store in memory, if you don't need them, don't retrieve them.</remarks>
		public NtfsReader(DriveInfo driveInfo, RetrieveMode retrieveMode)
		{
			if (driveInfo == null)
				throw new ArgumentNullException("driveInfo");

			_driveInfo = driveInfo;
			_retrieveMode = retrieveMode;

			_driveNameTrimmed = _driveInfo.Name.TrimEnd(new char[] { '\\' });

			StringBuilder builder = new StringBuilder(1024);
			GetVolumeNameForVolumeMountPoint(_driveInfo.RootDirectory.Name, builder, builder.Capacity);

			string volume = builder.ToString().TrimEnd(new char[] { '\\' });

			_volumeHandle =
				CreateFile(
					volume,
					FileAccess.Read,
					FileShare.All,
					IntPtr.Zero,
					FileMode.Open,
					0,
					IntPtr.Zero
					);

			if (_volumeHandle == null || _volumeHandle.IsInvalid)
				throw new IOException(
					string.Format(
						"Unable to open volume {0}. Make sure it exists and that you have Administrator privileges.",
						driveInfo
					)
				);

			using (_volumeHandle)
			{
				InitializeDiskInfo();

				_nodes = ProcessMft();
			}

			//cleanup anything that isn't used anymore
			_nameIndex = null;
			_volumeHandle = null;

			GC.Collect();
		}

		public IDiskInfo DiskInfo
		{
			get { return _diskInfo; }
		}

		/// <summary>
		/// Get all nodes under the specified rootPath.
		/// </summary>
		/// <param name="rootPath">The rootPath must at least contains the drive and may include any number of subdirectories. Wildcards aren't supported.</param>
		public List<INode> GetNodes(string rootPath)
		{
			return new List<INode>(EnumerateNodes(rootPath));
		}

		public List<INode> GetAllNodes()
		{
			return new List<INode>(EnumerateAllNodes());
		}

		public IEnumerable<INode> EnumerateNodes(string rootPath)
		{
			return _nodes.Where(
						(n, index) => (n.NameIndex != 0) &&
						(GetNodeFullNameCore((uint)index).StartsWith(rootPath, StringComparison.InvariantCultureIgnoreCase)))
					.Select((n, index) => new NodeWrapper(this, (uint)index, n));
		}

		public IEnumerable<INode> EnumerateAllNodes()
		{
			return _nodes.Where(n => n.NameIndex != 0).Select((n, index) => new NodeWrapper(this, (uint)index, n));
		}

		public byte[] GetVolumeBitmap()
		{
			return _bitmapData;
		}

		#region IDisposable Members

		public void Dispose()
		{
			if (_volumeHandle != null)
			{
				_volumeHandle.Dispose();
				_volumeHandle = null;
			}
		}

		#endregion
	}
}
