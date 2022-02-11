import { distinct } from '~app/utility/Utils';
import { IFramelineAppConfig, IFramelineConfig, IFramelineData, IFramelineEvent, IFramelineEventConfig } from '../FramelineTypes';
import { FramelineActions, IEnableEventPayload } from './FramelineActions';

const CONFIG_KEY = 'frameline.config';

export interface IFramelineState {
	config: IFramelineAppConfig;
	data: IFramelineData;
	originalData: IFramelineData;
	selectedEvent?: IFramelineEvent;
	eventTypes: IFramelineEventConfig[];
}

const generalEvent: IFramelineEventConfig = {
	Color: '#444444FF',
	Id: 'General Event'
};

const longFrameEvent: IFramelineEventConfig = {
	Color: '#FF0000FF',
	Id: '__LongFrame'
};

export function getEventConfig(eventId: string, config: IFramelineConfig): IFramelineEventConfig {
	if (eventId === '__LongFrame') {
		return longFrameEvent;
	}

	const configs = config.Events;

	const result = configs.find(evtConfig => evtConfig.Id === eventId);

	return result || config.GeneralEvent;
}

const defaultData: IFramelineData = {
	Config: {
		GeneralEvent: generalEvent,
		LongFrameTimeMS: 64,
		Events: []
	},
	Events: []
};

const defaultAppConfig: IFramelineAppConfig = {
	pixelsPerSecond: 100,
	minEventTime: 100,
	disabledEvents: [],
	viewMode: 'timeline'
};

const initialState: IFramelineState = {
	config: loadConfig(),
	data: defaultData,
	originalData: defaultData,
	eventTypes: []
};

const setEventEnable = (disabledEvents: string[], payload: IEnableEventPayload) => {
	const set = new Set(disabledEvents);
	const index = disabledEvents.indexOf(payload.eventId);

	if (payload.enabled && index > -1) {
		set.delete(payload.eventId);
	} else if (!payload.enabled && index === -1) {
		set.add(payload.eventId);
	}

	return Array.from(set);
};

const filterData = (events: IFramelineEvent[], config: IFramelineAppConfig): IFramelineEvent[] => {
	if (config.disabledEvents.length === 0) {
		return events;
	}

	return events.filter(evt => {
		return config.disabledEvents.indexOf(evt.EventType) === -1;
	});
};

const getDistinctEventTypes = (data: IFramelineData): IFramelineEventConfig[] => {
	return data.Events
		.map(evt => evt.EventType)
		.filter(distinct)
		.sort()
		.map(evt => {
			return {
				Name: evt,
				Id: evt,
				Color: getEventConfig(evt, data.Config).Color
			};
		});
};

function loadConfig(): IFramelineAppConfig {
	const configJson = localStorage.getItem(CONFIG_KEY);

	if (configJson) {
		try {
			const obj = JSON.parse(configJson);
			return {
				...defaultAppConfig,
				...obj as IFramelineAppConfig
			};
		} catch (err) {
			console.error(`Failed to parse old Config\n${err}`);
		}
	}

	return defaultAppConfig;
}

function saveConfig(config: IFramelineAppConfig) {
	const json = JSON.stringify(config);
	localStorage.setItem(CONFIG_KEY, json);
}

export function framelineReducer(state = initialState, action: FramelineActions): IFramelineState {
	switch (action.type) {
		case 'SET_DATA':
			return {
				...state,
				originalData: action.payload,
				data: {
					...action.payload,
					Events: filterData(action.payload.Events, state.config)
				},
				eventTypes: getDistinctEventTypes(action.payload)
			};
		case 'ENABLE_EVENT': {
			const config = {
				...state.config,
				disabledEvents: setEventEnable(state.config.disabledEvents, action.payload)
			};

			saveConfig(config);

			return {
				...state,
				data: {
					...state.originalData,
					Events: filterData(state.originalData.Events, config)
				},
				config
			};
		}
		case 'SET_SELECTED_EVENT': {
			return {
				...state,
				selectedEvent: action.payload
			};
		}
		case 'SET_CONFIG_OPTION': {
			const config = {
				...state.config,
				...action.payload
			};

			saveConfig(config);
			return {
				...state,
				config
			};
		}

		default:
			return state;
	}
}
