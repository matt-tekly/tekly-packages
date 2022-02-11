
import { HomeActions } from './HomeActions';
import { IInfoSummary } from '~app/utility/Types';

export interface IHomeState {
	info: IInfoSummary;
}

const initialState: IHomeState = {
	info: {
        Info: []
    }
};

export function homeReducer(state = initialState, action: HomeActions): IHomeState {
	switch (action.type) {
		case 'SET_INFO_SUMMARY':
			return {
				...state,
				info: action.payload
			};
		default:
			return state;
	}
}
