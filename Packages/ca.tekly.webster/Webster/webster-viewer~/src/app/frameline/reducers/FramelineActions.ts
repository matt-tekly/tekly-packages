import { IFramelineAppConfig, IFramelineData, IFramelineEvent, ViewMode } from '../FramelineTypes';

export interface IEnableEventPayload {
	eventId: string;
	enabled: boolean;
}

export function enableEvent(payload: IEnableEventPayload) {
	return {
		type: 'ENABLE_EVENT',
		payload
	} as const;
}

export function setConfigOption(config: Partial<IFramelineAppConfig>) {
	return {
		type: 'SET_CONFIG_OPTION',
		payload: config
	} as const;
}

export function setData(framelineData: IFramelineData) {
	return {
		type: 'SET_DATA',
		payload: framelineData
	} as const;
}

export function setSelectedEvent(framelineEvent?: IFramelineEvent) {
	return {
		type: 'SET_SELECTED_EVENT',
		payload: framelineEvent
	} as const;
}

export type FramelineActions = ReturnType<
	typeof enableEvent | typeof setData | typeof setConfigOption | typeof setSelectedEvent
>;
