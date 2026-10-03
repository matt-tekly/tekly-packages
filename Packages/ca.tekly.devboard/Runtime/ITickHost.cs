namespace Tekly.DevBoard
{
	/// <summary>
	/// Something that ticks the widgets inside it: a Board or a DevBoardPanel. Widgets tick with the nearest host
	/// above them.
	/// </summary>
	internal interface ITickHost
	{
		TickGroup TickGroup { get; }
	}
}
