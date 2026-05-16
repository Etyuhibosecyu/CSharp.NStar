namespace AvaloniaEdit.Document;

public class DataObjectCopyingEventArgs(IDataTransfer dataObject, bool isDragDrop) : RoutedEventArgs(DataObjectEx.DataObjectCopyingEvent)
{
	public bool CommandCancelled { get; private set; }
	public IDataTransfer DataObject { get; private set; } = dataObject;
	public bool IsDragDrop { get; private set; } = isDragDrop;

	public void CancelCommand() => CommandCancelled = true;
}