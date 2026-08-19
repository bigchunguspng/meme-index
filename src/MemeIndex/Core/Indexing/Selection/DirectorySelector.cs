using System.Text;
using MemeIndex.Utils;

namespace MemeIndex.Core.Indexing.Selection;

public static class DirectorySelector
{
	public static DirectoryResponse? View(string? path)
	{
		if (path == null)
		{
			if (Helpers.IsWindows) // list drives
				return new DirectoryResponse
				{
					F = [],
					C = Directory.GetLogicalDrives().Select(FileSystemAnchor.FromDrive),
				};
			else // ~
				path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		}
		else if (Directory.Exists(path).Janai()) return null;

		var directories = new List<FileSystemAnchor>();
		var file_counts = new Dictionary<string, int>();
		var nodes       = new DirectoryInfo(path) // todo fix bug: path "C:" -> info is about app dir
			.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly)
			.Select(entry => new
			{
				Entry       = entry,
				IsDirectory = entry.Attributes.HasFlag(FileAttributes.Directory),
				IsSymlink   = entry.LinkTarget != null,
				IsShortcut  = entry.Extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase),
			})
			.OrderByDescending(x => x.IsDirectory)
			.ThenBy(x => x.Entry.Name);

		foreach (var x in nodes)
		{
			if (x.IsDirectory || x.IsSymlink || x.IsShortcut)
			{
				var anchor
					= x.IsDirectory ? FileSystemAnchor.FromDirectory(x.Entry.FullName)
					: x.IsSymlink   ? FileSystemAnchor.FromSymlink  (x.Entry)
					:                 FileSystemAnchor.FromShortcut (x.Entry.FullName);

				var broken_shortcut = x.IsShortcut && anchor.T.IsNull_OrEmpty();
				if (broken_shortcut) CountFile("");
				else
				{
					var target_is_directory
						= x.IsDirectory
					   || new FileInfo(anchor.T).Attributes.HasFlag(FileAttributes.Directory);

					if (target_is_directory) directories.Add(anchor);
					else
						CountFile(x.IsSymlink ? anchor.T : x.Entry.FullName);
				}
			}
			else
				CountFile(x.Entry.FullName);
		}

		file_counts = file_counts
			.OrderBy(g => g.Key[0]) // ...extensions, unsupported
			.ThenByDescending(g => g.Value) // sort extensions by count
			.ToDictionary();
		file_counts.Add("total", file_counts.Values.Sum());

		return new DirectoryResponse
		{
			U = Path.GetDirectoryName(path),
			F = file_counts,
			C = directories,
		};

		void CountFile(string file_path)
		{
			var extension = Path.GetExtension(file_path);
			var supported = Indexing.SupportedExtensions.Contains(extension);
			var key = supported ? extension : "unsupported";

			if (file_counts.TryAdd(key, 1).Failed())
				file_counts[key]++;
		}
	}
}

public class DirectoryResponse
{
	public char    S { get; } = Path.DirectorySeparatorChar;

	public string? U { get; set; } // Up   (full path)

	public required Dictionary<string, int>       F { get; set; } // Files (count by type) (no recursion)
	public required IEnumerable<FileSystemAnchor> C { get; set; } // Child directories and links (full paths)
}

/// Represents a directory or a symlink.
public record struct FileSystemAnchor(string N, string T) // Name, Target (full path)
{
	public static FileSystemAnchor FromDrive
		(string x)
		=> new(x.TrimEnd('\\'), x);

	public static FileSystemAnchor FromDirectory
		(string x)
		=> new(Path.GetFileName(x), x);

	public static FileSystemAnchor FromShortcut
		(string x)
		=> new(Path.GetFileNameWithoutExtension(x), WindowsShortcutReader.GetTargetPath(x) ?? "");

	public static FileSystemAnchor FromSymlink
		(FileSystemInfo x)
		=> new(x.Name, x.LinkTarget!);
}

public static class WindowsShortcutReader
{
	/// Sauce: https://stackoverflow.com/questions/64126236
	public static string? GetTargetPath(FilePath shortcut)
	{
		try
		{
			return GetTargetPath_Internal(shortcut);
		}
		catch
		{
			LogError($"[WSR] INVALID SHORTCUT FOUND >> {shortcut}");
			return null;
		}
	}

	private static string GetTargetPath_Internal(FilePath shortcut)
	{
		using var reader = new BinaryReader(File.OpenRead(shortcut));
		reader.ReadBytes(20);
		var hasLinkTargetIDList = reader.ReadUInt32() & 1;
		if (hasLinkTargetIDList == 1)
		{
			reader.ReadBytes(52);
			reader.ReadBytes(reader.ReadUInt16());
		}
		var length = reader.ReadUInt32();
		reader.ReadBytes(12);
		var localBasePath = reader.ReadUInt32();
		reader.ReadBytes((int)localBasePath - 20);
		var size = length - localBasePath - 2;
		var path_bytes = reader.ReadBytes((int)size);
		return Encoding.UTF8.GetString(path_bytes);
	}
}