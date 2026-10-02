namespace Tekly.Leaf.Elements.Radios
{
	public interface ILeafRadioGroup
	{
		void OnOptionPressed(LeafRadioOption option);
		void OnOptionSetOn(LeafRadioOption option);

		/// <summary>
		/// The option was turned off directly (IsOn = false), not by the group.
		/// </summary>
		void OnOptionSetOff(LeafRadioOption option) { }
	}
}