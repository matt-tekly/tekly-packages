import Konva from 'konva';
import { IFramelineAppConfig, IFramelineConfig, IFramelineData, IFramelineEvent } from '~app/frameline/FramelineTypes';
import { renderSegments } from './SegmentRenderer';

const PADDING = 500;

export interface IFramelineRenderContext {
	data: IFramelineData;
	config: IFramelineConfig;
	appConfig: IFramelineAppConfig;

	stage: Konva.Stage;
	guideLayer: Konva.Layer;
	timelineLayer: Konva.Layer;

	setSelectedEvent: (event?: IFramelineEvent) => void;
}

export class CanvasController {

	private container: HTMLDivElement;

	private viewPortWidth: number;
	private viewPortHeight: number;

	private stage: Konva.Stage;
	private guideLayer: Konva.Layer;
	private timelineLayer: Konva.Layer;

	private stageWidth: number;
	private stageHeight: number;

	private setSelectedEvent: (event?: IFramelineEvent) => void;

	constructor(container: HTMLDivElement, setSelectedEvent: (event?: IFramelineEvent) => void) {
		this.container = container;
		this.setSelectedEvent = setSelectedEvent;

		this.stageWidth = this.viewPortWidth = container.scrollWidth;
		this.stageHeight = this.viewPortHeight = container.scrollHeight;

		this.stage = new Konva.Stage({
			container: this.container,
			width: this.stageWidth + PADDING * 2,
			height: this.stageHeight + PADDING * 2,
		});

		this.guideLayer = new Konva.Layer();
		this.stage.add(this.guideLayer);

		this.timelineLayer = new Konva.Layer();
		this.stage.add(this.timelineLayer);

		this.timelineLayer.draw();
	}

	public onScroll = (scrollContainer: HTMLDivElement) => {
		const dx = scrollContainer.scrollLeft - PADDING;
		const dy = scrollContainer.scrollTop - PADDING;

		this.stage.container().style.transform = `translate(${dx}px, ${dy}px)`;
		this.stage.x(-dx);
		this.stage.y(-dy);
		this.stage.batchDraw();
	}

	public onResizeSensor = (rect: DOMRectReadOnly) => {
		this.viewPortWidth = rect.width;
		this.viewPortHeight = rect.height;

		this.stageWidth = Math.max(this.stageWidth, this.viewPortWidth);
		this.stageHeight = Math.max(this.stageHeight, this.viewPortHeight);

		const size = {
			width: this.stageWidth + PADDING * 2,
			height: this.stageHeight + PADDING * 2,
		};

		this.stage.setSize(size);
	}

	public render(data: IFramelineData, config: IFramelineConfig, appConfig: IFramelineAppConfig) {

		const context: IFramelineRenderContext = {
			data,
			appConfig,
			config,
			stage: this.stage,
			guideLayer: this.guideLayer,
			timelineLayer: this.timelineLayer,
			setSelectedEvent: this.setSelectedEvent
		};

		return renderSegments(context);
	}
}
