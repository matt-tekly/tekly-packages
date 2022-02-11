import Konva from 'konva';
import { TimelineLayout } from '~app/frameline/TimelineLayout';
import { FRAME_EVENT_ID, HEADER_HEIGHT, SEGMENT_MARGIN, START_OFFSET } from '../FramelineConstants';
import { IFramelineRenderContext } from './CanvasController';
import { IDataSegment, segmentFramelineData } from './FramelineUtils';
import TimelineEvent from './TimelineEvent';

const SEGMENT_END_PADDING = 30;

export function renderSegments(context: IFramelineRenderContext) {
	context.timelineLayer.destroyChildren();

	const segments = segmentFramelineData(context.data);

	let offset = START_OFFSET;

	for (let i = 0; i < segments.length; i++) {
		const segment = segments[i];
		const layout = new TimelineLayout();

		for (let j = 0; j < segment.events.length; j++) {
			const evt = segment.events[j];

			if (evt.EventType === '__LongFrame') {
				continue;
			}

			const row = layout.getRow(evt, context.appConfig);
			TimelineEvent.drawSegmented(
				context.stage,
				context.timelineLayer,
				evt,
				row,
				context.appConfig.pixelsPerSecond,
				segment.startTimeMs,
				offset,
				context.config,
				context.setSelectedEvent
			);
		}

		const segmentLength = getSegmentLength(segment, context);
		offset += segmentLength + SEGMENT_MARGIN;
	}

	context.timelineLayer.batchDraw();

	renderSegmentsGuide(segments, context);

	return offset;
}

function getSegmentLength(segment: IDataSegment, context: IFramelineRenderContext) {
	return SEGMENT_END_PADDING + ((segment.endTimeMs - segment.startTimeMs) / 1000) * context.appConfig.pixelsPerSecond;
}

function renderSegmentsGuide(segments: IDataSegment[], context: IFramelineRenderContext) {
	context.guideLayer.destroyChildren();

	let offset = START_OFFSET;

	for (let i = 0; i < segments.length; i++) {
		const segment = segments[i];

		renderGuide(segment, context, offset);

		const segmentLength = getSegmentLength(segment, context);
		offset += segmentLength + SEGMENT_MARGIN;
	}

	context.guideLayer.batchDraw();
}

function renderGuide(segment: IDataSegment, context: IFramelineRenderContext, offset: number) {

	const width = getSegmentLength(segment, context);
	const height = 1000; // TODO: Get the height from the container

	// Segment background
	context.guideLayer.add(new Konva.Rect({
		x: offset,
		y: 0,
		fill: '#383b3d',
		width,
		height,
		strokeEnabled: true,
		strokeWidth: 2
	}));

	// Guide header background
	context.guideLayer.add(new Konva.Rect({
		x: offset,
		y: 0,
		fill: '#4B4F52',
		width,
		height: HEADER_HEIGHT,
		shadowEnabled: true,
		shadowBlur: 5,
		shadowColor: 'rgba(0,0,0,0.75)',
		shadowOffset: { x: 0, y: 3 }
	}));

	const pixelsPerSecond = context.appConfig.pixelsPerSecond;
	const timeBetweenMarkers = 200 / pixelsPerSecond;
	const duration = (segment.endTimeMs - segment.startTimeMs) / 1000;
	const numberOfLines = duration / timeBetweenMarkers;

	const startTime = segment.startTimeMs / 1000;

	for (let i = 0; i < numberOfLines; i++) {

		const x = offset + i * pixelsPerSecond * timeBetweenMarkers;
		// Guide Line
		context.guideLayer.add(new Konva.Rect({
			x,
			y: 0,
			fill: 'rgba(0,0,0,0.25)',
			width: 1,
			height: 1000,
		}));

		// Time Label
		context.guideLayer.add(new Konva.Text({
			text: (startTime + i * timeBetweenMarkers).toFixed(2) + 's',
			x: x + 2,
			y: 2,
			fontFamily: `'Segoe UI', 'Helvetica'`,
			fill: '#9B9DA1',
			fontSize: 11,
			shadowColor: 'rgba(0,0,0,.5)',
			shadowOffset: { x: 1, y: 1 },
		}));
	}

	for (let j = 0; j < segment.events.length; j++) {
		const evt = segment.events[j];

		if (evt.EventType !== '__LongFrame') {
			continue;
		}

		const timeStart = (evt.StartTime - segment.startTimeMs);
		const length = evt.EndTime - evt.StartTime;

		const rect = new Konva.Rect({
			x: timeStart / 1000 * pixelsPerSecond + offset,
			y: HEADER_HEIGHT - 3,
			fill: 'rgba(255,0,0,1)',
			width: Math.max((length / 1000 * pixelsPerSecond), 10),
			height: 3,
		});

		context.guideLayer.add(rect);
	}

	context.guideLayer.draw();
}
