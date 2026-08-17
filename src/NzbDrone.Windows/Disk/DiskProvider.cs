using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Instrumentation;

namespace NzbDrone.Windows.Disk
{
    public class DiskProvider : DiskProviderBase
    {
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(DiskProvider));

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
        out ulong lpFreeBytesAvailable,
        out ulong lpTotalNumberOfBytes,
        out ulong lpTotalNumberOfFreeBytes);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetFileInformationByHandle(SafeFileHandle hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);

        [StructLayout(LayoutKind.Sequential)]
        private struct BY_HANDLE_FILE_INFORMATION
        {
            public uint FileAttributes;
            public FILETIME CreationTime;
            public FILETIME LastAccessTime;
            public FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        public DiskProvider()
        : this(new FileSystem())
        {
        }

        public DiskProvider(IFileSystem fileSystem)
        : base(fileSystem)
        {
        }

        public override IMount GetMount(string path)
        {
            var reparsePoint = GetReparsePoint(path);

            return reparsePoint ?? base.GetMount(path);
        }

        public override string GetPathRoot(string path)
        {
            Ensure.That(path, () => path).IsValidPath(PathValidationType.CurrentOs);

            var reparsePoint = GetReparsePoint(path);

            return reparsePoint?.RootDirectory ?? base.GetPathRoot(path);
        }

        public override long? GetAvailableSpace(string path)
        {
            Ensure.That(path, () => path).IsValidPath(PathValidationType.CurrentOs);

            var root = GetPathRoot(path);

            if (!FolderExists(root))
            {
                throw new DirectoryNotFoundException(root);
            }

            return DriveFreeSpaceEx(root);
        }

        public override void InheritFolderPermissions(string filename)
        {
            Ensure.That(filename, () => filename).IsValidPath(PathValidationType.CurrentOs);

            var fileInfo = new FileInfo(filename);
            var fs = fileInfo.GetAccessControl(AccessControlSections.Access);
            fs.SetAccessRuleProtection(false, false);
            fileInfo.SetAccessControl(fs);
        }

        public override void SetEveryonePermissions(string filename)
        {
            var accountSid = WellKnownSidType.WorldSid;
            var rights = FileSystemRights.Modify;
            var controlType = AccessControlType.Allow;

            try
            {
                var sid = new SecurityIdentifier(accountSid, null);

                var directoryInfo = new DirectoryInfo(filename);
                var directorySecurity = directoryInfo.GetAccessControl(AccessControlSections.Access);

                var rules = directorySecurity.GetAccessRules(true, false, typeof(SecurityIdentifier));

                if (rules.OfType<FileSystemAccessRule>().Any(acl => acl.AccessControlType == controlType && (acl.FileSystemRights & rights) == rights && acl.IdentityReference.Equals(sid)))
                {
                    return;
                }

                var accessRule = new FileSystemAccessRule(sid,
                                                          rights,
                                                          InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                                                          PropagationFlags.InheritOnly,
                                                          controlType);

                directorySecurity.ModifyAccessRule(AccessControlModification.Add, accessRule, out var modified);

                if (modified)
                {
                    directoryInfo.SetAccessControl(directorySecurity);
                }
            }
            catch (Exception e)
            {
                Logger.Warn(e, "Couldn't set permission for {0}. account:{1} rights:{2} accessControlType:{3}", filename, accountSid, rights, controlType);
                throw;
            }
        }

        public override void SetFilePermissions(string path, string mask, string group)
        {
        }

        public override void SetPermissions(string path, string mask, string group)
        {
        }

        public override void CopyPermissions(string sourcePath, string targetPath)
        {
        }

        public override long? GetTotalSize(string path)
        {
            Ensure.That(path, () => path).IsValidPath(PathValidationType.CurrentOs);

            var root = GetPathRoot(path);

            if (!FolderExists(root))
            {
                throw new DirectoryNotFoundException(root);
            }

            return DriveTotalSizeEx(root);
        }

        private static long DriveFreeSpaceEx(string folderName)
        {
            Ensure.That(folderName, () => folderName).IsValidPath(PathValidationType.CurrentOs);

            if (!folderName.EndsWith("\\"))
            {
                folderName += '\\';
            }

            if (GetDiskFreeSpaceEx(folderName, out var free, out var dummy1, out var dummy2))
            {
                return (long)free;
            }

            return 0;
        }

        private static long DriveTotalSizeEx(string folderName)
        {
            Ensure.That(folderName, () => folderName).IsValidPath(PathValidationType.CurrentOs);

            if (!folderName.EndsWith("\\"))
            {
                folderName += '\\';
            }

            if (GetDiskFreeSpaceEx(folderName, out var dummy1, out var total, out var dummy2))
            {
                return (long)total;
            }

            return 0;
        }

        public override bool TryCreateHardLink(string source, string destination)
        {
            try
            {
                if (source.Length > 256 && !source.StartsWith(@"\\?\"))
                {
                    source = @"\\?\" + source;
                }

                return CreateHardLink(destination, source, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, string.Format("Hardlink '{0}' to '{1}' failed.", source, destination));
                return false;
            }
        }

        public override int GetHardLinkCount(string path)
        {
            try
            {
                if (path.Length > 256 && !path.StartsWith(@"\\?\"))
                {
                    path = @"\\?\" + path;
                }

                using (var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    if (GetFileInformationByHandle(handle, out var fileInfo))
                    {
                        return (int)fileInfo.NumberOfLinks;
                    }
                }

                return 1;
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, string.Format("Unable to get hard link count for '{0}'.", path));
                return 1;
            }
        }

        private IMount GetReparsePoint(string path)
        {
            if (!Directory.Exists(path))
            {
                return null;
            }

            var di = new DirectoryInfo(path);
            var isReparsePoint = di.Attributes.HasFlag(FileAttributes.ReparsePoint);

            while (!isReparsePoint && (di = di.Parent) != null)
            {
                isReparsePoint = di.Attributes.HasFlag(FileAttributes.ReparsePoint);
            }

            if (isReparsePoint)
            {
                return new FolderMount(di);
            }

            return null;
        }
    }
}
