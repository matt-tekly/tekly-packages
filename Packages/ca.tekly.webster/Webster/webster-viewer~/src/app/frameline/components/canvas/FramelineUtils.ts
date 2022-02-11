import { IFramelineData, IFramelineEvent } from '~app/frameline/FramelineTypes';

const LARGE_TIME_GAP_MS = 3000;

export interface IDataSegment {
	startTimeMs: number;
	endTimeMs: number;

	events: IFramelineEvent[];
}

export function segmentFramelineData(data: IFramelineData) {
	if (data.Events.length === 0) {
		return [];
	}

	const segments: IDataSegment[] = [];

	const events = data.Events;

	let endTime = data.Events[0].EndTime;
	let startIndex = 0;

	for (let i = startIndex + 1; i < events.length; i++) {
		const evt = events[i];

		if (evt.StartTime - endTime > LARGE_TIME_GAP_MS) {
			segments.push(createSegment(startIndex, i, events));
			startIndex = i;
			endTime = evt.EndTime;
		} else {
			endTime = Math.max(endTime, evt.EndTime);
		}
	}

	segments.push(createSegment(startIndex, events.length, events));

	return segments;
}

function createSegment(startIndex: number, endIndex: number, events: IFramelineEvent[]): IDataSegment {
	let endTime = 0;

	for (let i = startIndex; i < endIndex; i++) {
		endTime = Math.max(endTime, events[i].EndTime);
	}

	return {
		startTimeMs: events[startIndex].StartTime,
		endTimeMs: endTime,
		events: events.slice(startIndex, endIndex)
	};
}
