
import { IAssetsSummary } from '../Types';
import { AssetsActions } from './AssetsActions';

export interface IAssetsState {
	assets: IAssetsSummary;
}

const initialState: IAssetsState = {
	assets: {
		AudioClips: [],
		Materials: [],
		Meshes: [],
		Shaders: [],
		Sprites: [],
		Textures: []
	}
};

export function assetsReducer(state = initialState, action: AssetsActions): IAssetsState {
	switch (action.type) {
		case 'SET_ASSETS':
			return {
				...state,
				assets: action.payload
			};
		default:
			return state;
	}
}
