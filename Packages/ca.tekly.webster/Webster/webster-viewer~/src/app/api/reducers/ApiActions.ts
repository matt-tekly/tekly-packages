import { IRouteInfo, IRouteResponse, IRouteState } from '../ApiTypes';

export function setRouteInfo(routeInfo: IRouteInfo) {
	return {
		type: 'SET_ROUTE_INFO',
		payload: routeInfo
	} as const;
}

export function setRouteState(route: string, routeState: IRouteState) {
	return {
		type: 'SET_ROUTE_STATE',
		payload: {route, routeState}
	} as const;
}

export function setRouteProperty(route: string,property: string, value: any) {
	return {
		type: 'SET_ROUTE_VALUE',
		payload: {route, property, value}
	} as const;
}

export function setRouteResponse(route: string, response: IRouteResponse) {
	return {
		type: 'SET_ROUTE_RESPONSE',
		payload: {route, response}
	} as const;
}

export type ApiActions = ReturnType<
	typeof setRouteInfo | typeof setRouteState | typeof setRouteProperty | typeof setRouteResponse
>;
