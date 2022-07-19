using Microsoft.Build.Framework;
using System;
using System.IO;
using System.Linq;

namespace Packaging.Targets
{
    public static class TaskItemExtensions
    {
        public static bool IsPublished(this ITaskItem item)
        {
            if (item == null)
            {
                return false;
            }

            if (!item.MetadataNames.OfType<string>().Contains("CopyToPublishDirectory"))
            {
                return false;
            }

            var copyToPublishDirectoryValue = item.GetMetadata("CopyToPublishDirectory");
            CopyToDirectoryValue copyToPublishDirectory;

            if (!Enum.TryParse<CopyToDirectoryValue>(copyToPublishDirectoryValue, out copyToPublishDirectory))
            {
                return false;
            }

            return copyToPublishDirectory != CopyToDirectoryValue.DoNotCopy;
        }

        public static string GetPublishedPath(this ITaskItem item)
        {
            if (item == null)
            {
                return null;
            }

            var link = item.GetMetadata("Link");
            if (!string.IsNullOrEmpty(link))
            {
                return link.Replace("\\", "/");
            }

            var relativeDirectory = item.GetMetadata("RelativeDir");
            var filename = item.GetMetadata("FileName");
            var extension = item.GetMetadata("Extension");

            return Path.Combine(relativeDirectory, $"{filename}{extension}").Replace("\\", "/");
        }

        public static string GetLinuxPath(this ITaskItem item)
        {
            return TryGetValue(item, "LinuxPath");
        }

        public static string GetLinuxFileMode(this ITaskItem item)
        {
            return TryGetValue(item, "LinuxFileMode");
        }

        public static string GetOwner(this ITaskItem item)
        {
            return TryGetValue(item, "Owner", "root");
        }

        public static string GetGroup(this ITaskItem item)
        {
            return TryGetValue(item, "Group", "root");
        }

        public static string GetVersion(this ITaskItem item)
        {
            return TryGetValue(item, "Version", null);
        }

        public static bool GetRemoveOnUninstall(this ITaskItem item)
        {
            var valueString = TryGetValue(item, "RemoveOnUninstall", "false");
            bool value;

            if (!bool.TryParse(valueString, out value))
            {
                return false;
            }

            return value;
        }

        private static string TryGetValue(ITaskItem item, string name, string @default = null)
        {
            if (item == null)
            {
                return @default;
            }

            if (!item.MetadataNames.OfType<string>().Contains(name))
            {
                return @default;
            }

            var linuxPath = item.GetMetadata(name);

            return linuxPath;
        }
    }
}
