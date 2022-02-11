import { ThunkDispatch } from 'redux-thunk';

import { GameFetcher } from '~app/utility/Fetcher';
import { IHierarchy } from '../GameObjectTypes';

export function setHierarchy(hierarchy: IHierarchy) {
	return {
		type: 'SET_HIERARCHY',
		payload: hierarchy
	} as const;
}

export function refreshHierarchy() {
	return async (dispatch: ThunkDispatch<{}, {}, GameObjectActions>) => {
		const resp = await GameFetcher.get('api/hierarchy', 'Json');
		const hierarchy = resp.body as IHierarchy;
		dispatch(setHierarchy(hierarchy));
	};
}

export function setGameObjectActive(instanceId: number, active: boolean) {
	return async (dispatch: ThunkDispatch<{}, {}, GameObjectActions>) => {
		const resp = await GameFetcher.put(`api/gameobject/setactive?instanceId=${instanceId}&active=${active}`, 'Json');
		const hierarchy = resp.body as IHierarchy;
		dispatch(setHierarchy(hierarchy));
	};
}

export function setComponentEnabled(instanceId: number, enabled: boolean) {
	return async (dispatch: ThunkDispatch<{}, {}, GameObjectActions>) => {
		const resp = await GameFetcher.put(`api/component/setenabled?instanceId=${instanceId}&enabled=${enabled}`, 'Json');
		const hierarchy = resp.body as IHierarchy;
		dispatch(setHierarchy(hierarchy));
	};
}

export type GameObjectActions = ReturnType<
	typeof setHierarchy
>;
