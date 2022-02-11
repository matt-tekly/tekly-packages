import { ThunkDispatch } from 'redux-thunk';
import { GameFetcher } from '~app/utility/Fetcher';
import { IDirectoryResponse, IDiskEntrySummary } from '../DiskTypes';

export function setDirectoryResponse(directory: IDirectoryResponse) {
	return {
		type: 'SET_DIRECTORY_RESPONSE',
		payload: directory
	} as const;
}

export function refreshDirectories() {
	return async (dispatch: ThunkDispatch<{}, {}, DiskActions>) => {
		const resp = await GameFetcher.get('api/disk/', 'Json');
		const directoryResponse = resp.body as IDirectoryResponse;

		dispatch(setDirectoryResponse(directoryResponse));
	};
}

export function setSelectedEntry(entry: IDiskEntrySummary) {
	return {
		type: 'SET_SELECTED_ENTRY',
		payload: {
			entryPath: entry.Path
		}
	} as const;
}

export function setSelectedPath(path: string) {
	return {
		type: 'SET_SELECTED_PATH',
		payload: {
			entryPath: path
		}
	} as const;
}

export function setFilter(filter: string) {
	return {
		type: 'SET_FILTER',
		payload: filter
	} as const;
}

export function deleteEntry(path: string) {
	return async (dispatch: ThunkDispatch<{}, {}, DiskActions>) => {
		const result = await GameFetcher.delete(`api/disk${path}`);
		if (!result.response.ok) {
			console.log('failed to delete file');
		}

		dispatch(setSelectedPath(''));
		dispatch(refreshDirectories());
	};
}

export type DiskActions = ReturnType<
	typeof setDirectoryResponse | typeof setFilter | typeof setSelectedEntry | typeof setSelectedPath
>;
