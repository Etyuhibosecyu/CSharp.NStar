using System;

namespace AvaloniaEdit.Document;

/// <summary>
/// Provides data for the <see cref="ITextEditorComponent.DocumentChanged"/> event.
/// </summary>
public class DocumentChangedEventArgs(TextDocument oldDocument, TextDocument newDocument) : EventArgs
	{
	/// <summary>
	/// Gets the old TextDocument.
	/// </summary>
	public TextDocument OldDocument { get; private set; } = oldDocument;
	/// <summary>
	/// Gets the new TextDocument.
	/// </summary>
	public TextDocument NewDocument { get; private set; } = newDocument;
}
