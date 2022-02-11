import { IHierarchy } from '../GameObjectTypes';
import { GameObjectActions } from './GameObjectActions';

export interface IGameObjectState {
	hierarchy: IHierarchy;
}

const initialState: IGameObjectState = {
	hierarchy: {
		GameObjects: []
	}
};

export function gameObjectReducer(state = initialState, action: GameObjectActions): IGameObjectState {
	switch (action.type) {
		case 'SET_HIERARCHY':
			return {
				...state,
				hierarchy: action.payload
			};
		default:
			return state;
	}
}
