import { ThunkDispatch } from 'redux-thunk';

import { GameFetcher } from '~app/utility/Fetcher';
import { IAssetsSummary } from '../Types';

export function setAssets(assets: IAssetsSummary) {
	return {
		type: 'SET_ASSETS',
		payload: assets
	} as const;
}

export function refreshAssets() {
	return async (dispatch: ThunkDispatch<{}, {}, AssetsActions>) => {
		const resp = await GameFetcher.get('api/assets', 'Json');
		const assets = resp.body as IAssetsSummary;
		dispatch(setAssets(assets));
	};
}

export type AssetsActions = ReturnType<
	typeof setAssets
>;
