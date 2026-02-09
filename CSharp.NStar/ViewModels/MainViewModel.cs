using Avalonia.Platform.Storage;

namespace CSharp.NStar.ViewModels;

public class MainViewModel : ViewModelBase
{
	public static List<FilePickerFileType> NStarFileTypeFiter { get; } = [new("C#.NStar code files")
	{
		Patterns = ["*.n-star-alpha"], AppleUniformTypeIdentifiers = ["UTType.Item"],
		MimeTypes = ["multipart/mixed"]
	}];
}
