import { CustomActions } from "./customActions";

export interface ICustomState {
	thing: string;
}

const initialState: ICustomState = {
    thing: 'thing'
}

export function customReducer(state = initialState, action: CustomActions): ICustomState {
	switch (action.type) {
        case 'SET_CUSTOM_THING':
			return {
				...state,
				thing: action.payload
			};
		default:
			return state;
	}
}
