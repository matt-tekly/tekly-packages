import { ThunkDispatch } from 'redux-thunk';
import { GameFetcher } from '~app/utility/Fetcher';
import { IDictionary } from '~app/utility/Types';
import { IRouteDescriptor, IRouteInfo, IRouteResponse, IRouteState } from '../ApiTypes';
import { ApiActions, setRouteInfo, setRouteResponse } from './ApiActions';

export interface IApiState {
	routeInfo: IRouteInfo;
    routeStates: IDictionary<IRouteState>;
    routeResponses: IDictionary<IRouteResponse>;
}

const initialState: IApiState = {
	routeInfo: { Routes: [] },
    routeStates: {},
    routeResponses: {}
};

export function refreshRoutes() {
	return async (dispatch: ThunkDispatch<{}, {}, ApiActions>) => {
		const resp = await GameFetcher.get('api/routes', 'Json');
		const routeInfo = resp.body as IRouteInfo;
		routeInfo.Routes = routeInfo.Routes.sort((x, y) => x.Path.localeCompare(y.Path));

		dispatch(setRouteInfo(routeInfo));
	};
}

export function executeRoute(route: IRouteDescriptor, properties: IDictionary<any>) {
	return async (dispatch: ThunkDispatch<{}, {}, ApiActions>) => {

		let params = '?';

		Object.keys(properties).map((key, index) => {
			const value = properties[key];

			if (value !== '') {
				if (index > 0) {
					params += '&';
				}

				params += `${key}=${value}`;
			}
		});

		params = params.length === 1 ? '' : params;

		const resultType = getResultType(route.ReturnType);
		const resp = await GameFetcher.do(route.Path + params, route.Verb, resultType);

		dispatch(setRouteResponse(
			route.Path + route.Verb,
			{
				statusText: resp.response.statusText,
				result: resp.body
			}
		));
	};
}


function getResultType(returnType: string)  {
	switch (returnType) {
		case 'object':
			return 'Json';
		case 'string':
			return 'Text';
		case 'void':
			return 'None';
		default:
			return 'None';
	}
}

function getState(path: string, def: IRouteState): IRouteState {
	let data = localStorage.getItem(path);
	
	if (data) {
		return JSON.parse(data) as IRouteState;
	}

	return def;
}

function setState(path: string, state: IRouteState) {
	localStorage.setItem(path, JSON.stringify(state));
}

export function apiReducer(state = initialState, action: ApiActions): IApiState {
	switch (action.type) {
		case 'SET_ROUTE_INFO':
			const routeStates: IDictionary<IRouteState> = {};
			
			action.payload.Routes.forEach(route => {
				let routeState: IRouteState = {
					values: {}
				};
				
				route.QueryParams.forEach(param => {
					routeState.values[param.Name] = param.DefaultValue;
				});

				const path = route.Path + route.Verb; 
				routeStates[path] = getState(path, routeState);
			});
			
			return {
				...state,
				routeInfo: action.payload,
				routeStates: routeStates
			};
		case 'SET_ROUTE_STATE':
			return {
				...state,
				routeStates: {
                    ...state.routeStates,
                    [action.payload.route]: action.payload.routeState
                }
			};
		case 'SET_ROUTE_VALUE': 
			const {route, property, value} = action.payload;
			let routeState = state.routeStates[route];
			routeState = {
				...routeState,
				values: {
					...routeState.values,
					[property]: value
				}
			};

			setState(route, routeState);

			return {
				...state,
				routeStates: {
					...state.routeStates,
					[route]: routeState
				}
			}
		case 'SET_ROUTE_RESPONSE': 
			return {
				...state,
				routeResponses: {
					...state.routeResponses,
					[action.payload.route]: action.payload.response
				}
			}
		default:
			return state;
	}
}
