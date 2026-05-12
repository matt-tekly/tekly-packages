namespace Tekly.Leaf.Elements
{
	public interface ILeafButton
	{
		SelectableSelectedEvent OnSelected { get; }
		ButtonClickedEvent OnClicked { get; }
		bool interactable { get; set; }
	}
}