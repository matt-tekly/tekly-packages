
export type ViewMode = 'timeline' | 'table';

export interface IFramelineData {
	Config: IFramelineConfig;
	Events: IFramelineEvent[];
}

export interface IFramelineEvent {
	Id: string;
	EventType: string;
	StartFrame: number;
	EndFrame: number;

	StartTime: number;
	EndTime: number;
}

export interface IFramelineConfig {
	LongFrameTimeMS: number;
	GeneralEvent: IFramelineEventConfig;
	Events: IFramelineEventConfig[];
}

export interface IFramelineEventConfig {
	Id: string;
	Color: string;
}

export interface IFramelineAppConfig {
	pixelsPerSecond: number;
	minEventTime: number;
	disabledEvents: string[];
	viewMode: ViewMode;
}
