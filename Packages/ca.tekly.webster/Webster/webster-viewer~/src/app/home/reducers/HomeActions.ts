import { ThunkDispatch } from 'redux-thunk';

import { GameFetcher } from '~app/utility/Fetcher';
import { IInfoSummary } from '~app/utility/Types';

export function setInfoSummary(info: IInfoSummary) {
	return {
		type: 'SET_INFO_SUMMARY',
		payload: info
	} as const;
}

export function refreshInfoSummary() {
	return async (dispatch: ThunkDispatch<{}, {}, HomeActions>) => {
		const resp = await GameFetcher.get('api/info', 'Json');
		const assets = resp.body as IInfoSummary;
		dispatch(setInfoSummary(assets));
	};
}

export type HomeActions = ReturnType<
	typeof setInfoSummary
>;
