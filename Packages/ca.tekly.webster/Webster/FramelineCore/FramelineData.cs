//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System;
using System.Collections.Generic;

namespace Tekly.Webster.FramelineCore
{
	[Serializable]
	public class FramelineData
	{
		public FramelineConfig Config;
		public List<FrameEvent> Events;
	}
}