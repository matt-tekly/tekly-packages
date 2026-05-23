namespace Tekly.Leaf.Elements.Radios
{
	public interface ILeafRadioGroup
	{
		void OnOptionPressed(LeafRadioOption option);
		void OnOptionSetOn(LeafRadioOption option);
	}
}