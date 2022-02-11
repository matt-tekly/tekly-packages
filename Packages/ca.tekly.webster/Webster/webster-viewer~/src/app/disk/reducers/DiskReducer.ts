import { IDictionary } from '~app/utility/Types';
import { IDirectoryResponse, IDirectorySummary, IDiskEntrySummary } from '../DiskTypes';
import { DiskActions } from './DiskActions';

export interface IDiskState {
	directory: IDirectorySummary;
	entries: IDiskEntrySummary[];
	entryMap: IDictionary<IDiskEntrySummary>;
	filterText: string;
	selectedEntry: string;
}

const defaultDirectory: IDirectorySummary = {
	Directories: [],
	Files: [],
	Name: '',
	Path: '',
	Type: 'Directory'
};

const initialState: IDiskState = {
	directory: defaultDirectory,
	entries: [],
	entryMap: {},
	filterText: '',
	selectedEntry: ''
};

function setDirectoryResponse(state: IDiskState, response: IDirectoryResponse): IDiskState {
	const directory = response.Directory;

	const entryMap: IDictionary<IDiskEntrySummary> = {};
	toDictionary(entryMap, directory);
	const entries = Object.keys(entryMap).map(k => entryMap[k]);

	return {
		...state,
		directory,
		entryMap,
		entries
	};
}

function toDictionary(dictionary: IDictionary<IDiskEntrySummary>, dir: IDirectorySummary) {
	dir.Directories.forEach(d => {
		dictionary[d.Path] = d;
		toDictionary(dictionary, d);
	});

	dir.Files.forEach(f => {
		dictionary[f.Path] = f;
	});
}

export function diskReducer(state = initialState, action: DiskActions): IDiskState {
	switch (action.type) {
		case 'SET_DIRECTORY_RESPONSE':
			return setDirectoryResponse(state, action.payload);
		case 'SET_SELECTED_ENTRY':
			return {
				...state,
				selectedEntry: action.payload.entryPath,
			};
		case 'SET_FILTER':
			return {
				...state,
				filterText: action.payload,
			};
	}

	return state;
}
