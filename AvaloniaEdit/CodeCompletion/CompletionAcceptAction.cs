global using Avalonia;
global using Avalonia.Controls;
global using Avalonia.Controls.Documents;
global using Avalonia.Controls.Presenters;
global using Avalonia.Controls.Primitives;
global using Avalonia.Controls.Shapes;
global using Avalonia.Data.Converters;
global using Avalonia.Input;
global using Avalonia.Input.Platform;
global using Avalonia.Input.TextInput;
global using Avalonia.Interactivity;
global using Avalonia.LogicalTree;
global using Avalonia.Markup.Xaml.Templates;
global using Avalonia.Media;
global using Avalonia.Media.Immutable;
global using Avalonia.Media.TextFormatting;
global using Avalonia.Metadata;
global using Avalonia.Reactive;
global using Avalonia.Threading;
global using Avalonia.VisualTree;
global using AvaloniaEdit.CodeCompletion;
global using AvaloniaEdit.Document;
global using AvaloniaEdit.Editing;
global using AvaloniaEdit.Highlighting;
global using AvaloniaEdit.Highlighting.Xshd;
global using AvaloniaEdit.Indentation;
global using AvaloniaEdit.Rendering;
global using AvaloniaEdit.Search;
global using AvaloniaEdit.Utils;
global using System;
global using System.Collections;
global using System.Collections.Generic;
global using System.Collections.Immutable;
global using System.Collections.ObjectModel;
global using System.Collections.Specialized;
global using System.ComponentModel;
global using System.Diagnostics;
global using System.Diagnostics.CodeAnalysis;
global using System.Globalization;
global using System.IO;
global using System.Linq;
global using System.Net;
global using System.Reflection;
global using System.Runtime.CompilerServices;
global using System.Runtime.InteropServices;
global using System.Text;
global using System.Text.RegularExpressions;
global using System.Threading;
global using System.Web;
global using System.Windows.Input;
global using System.Xml;
global using LogicalDirection = AvaloniaEdit.Document.LogicalDirection;
global using SpanStack = System.Collections.Immutable.ImmutableStack<AvaloniaEdit.Highlighting.HighlightingSpan>;

namespace AvaloniaEdit.CodeCompletion;

/// <summary>
/// Defines the pointer action used to request the insertion of a completion item.
/// </summary>
public enum CompletionAcceptAction
{
	/// <summary>
	/// Insert the completion item when the pointer is pressed. (This option makes the completion
	/// list behave similar to the completion list in Visual Studio Code.)
	/// </summary>
	PointerPressed,

	/// <summary>
	/// Insert the completion item when the pointer is pressed. (This option makes the completion
	/// list behave similar to a context menu.)
	/// </summary>
	PointerReleased,

	/// <summary>
	/// Insert the code completion item when the item is double-tapped. (This option makes the
	/// completion list behave similar to the completion list in Visual Studio.)
	/// </summary>
	DoubleTapped
}
