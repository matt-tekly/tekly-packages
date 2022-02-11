import Konva from 'konva';

import { IFramelineConfig, IFramelineEvent } from '../../FramelineTypes';
import { getEventConfig } from '../../reducers/FramelineReducer';

import { getDivergentColor } from '~app/utility/Colors';
import { ROW_HEIGHT, SPACING, TIMELINE_EVENT_START_Y } from '../FramelineConstants';

const MIN_WIDTH = 10;

const TEXT_FILL = 'rgba(255,255,255,0.75)';
const TEXT_FILL_SELECTED = 'rgba(255,255,255,1)';

export interface IRect {
	x: number;
	y: number;
	width: number;
	height: number;
}

export default class TimelineEvent {

	private static drawEvent(
			evt: IFramelineEvent,
			color: string,
			setSelectedEvent: (event?: IFramelineEvent) => void,
			fullRect: IRect,
			stage: Konva.Stage,
			layer: Konva.Layer) {

		const group = new Konva.Group({
			...fullRect
		});

		const rect = new Konva.Rect({
			cornerRadius: 2,
			width: fullRect.width,
			height: fullRect.height,
			fill: color,
			stroke: 'rgba(0,0,0,0.25)',
			strokeWidth: 1,
			shadowEnabled: true,
			shadowBlur: 3,
			shadowColor: 'rgba(0,0,0,.5)',
			shadowOffset: { x: 0, y: 1 }
		});

		const text = new Konva.Text({
			text: evt.Id,
			width: fullRect.width - 2,
			height: fullRect.height,
			x: 2,
			y: 3,
			fill: TEXT_FILL,
			fontSize: ROW_HEIGHT - 4,
			fontFamily: `'Segoe UI', 'Helvetica'`,
			shadowColor: 'rgba(0,0,0,.5)',
			shadowOffset: { x: 1, y: 2 },
			strokeEnabled: true,
			wrap: 'none'
		});

		group.add(rect);
		group.add(text);

		layer.add(group);

		group.on('mouseenter', () => {
			stage.container().style.cursor = 'pointer';
			rect.stroke('rgba(0,0,0,1)');
			text.fill(TEXT_FILL_SELECTED);
			layer.draw();
		});

		group.on('click', () => {
			setSelectedEvent(evt);
		});

		group.on('mouseleave', () => {
			stage.container().style.cursor = 'default';
			rect.stroke('rgba(0,0,0,0.25)');
			text.fill(TEXT_FILL);
			layer.draw();
		});
	}

	public static drawSegmented(
						stage: Konva.Stage,
						layer: Konva.Layer,
						evt: IFramelineEvent,
						row: number,
						pixelsPerSecond: number,
						startTime: number,
						offset: number,
						config: IFramelineConfig,
						setSelectedEvent: (event?: IFramelineEvent) => void) {

		const timeStart = (evt.StartTime - startTime);
		const length = evt.EndTime - evt.StartTime;

		const fullRect = {
			x: timeStart / 1000 * pixelsPerSecond + offset,
			y: TIMELINE_EVENT_START_Y + (row * ROW_HEIGHT) + (row * SPACING),
			width: Math.max((length / 1000 * pixelsPerSecond), MIN_WIDTH),
			height: ROW_HEIGHT
		};

		const evtConfig = getEventConfig(evt.EventType, config);
		const color = evtConfig.Color;

		// const color = getDivergentColor(evt.EventType);
		TimelineEvent.drawEvent(evt, color, setSelectedEvent, fullRect, stage, layer);
	}
}
