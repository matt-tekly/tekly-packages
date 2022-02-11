import { applyMiddleware, combineReducers, createStore } from 'redux';
import thunkMiddleware from 'redux-thunk';

import { composeWithDevTools } from 'redux-devtools-extension';

import { assetsReducer } from './assets/reducers/AssetsReducer';
import { diskReducer } from './disk/reducers/DiskReducer';
import { framelineReducer } from './frameline/reducers/FramelineReducer';
import { gameObjectReducer } from './gameobject/reducers/GameObjectReducer';
import { homeReducer } from './home/reducers/HomeReducer';
import { customReducer } from './custom/reducers/customReducer';
import { apiReducer } from './api/reducers/ApiReducer';

const rootReducer = combineReducers({
	home: homeReducer,
	assets: assetsReducer,
	api: apiReducer,
	disk: diskReducer,
	frameline: framelineReducer,
	gameObject: gameObjectReducer,
	custom: customReducer
});

export default function configureStore() {
	const middlewares = [thunkMiddleware];
	const middleWareEnhancer = applyMiddleware(...middlewares);

	const store = createStore(
		rootReducer,
		composeWithDevTools(middleWareEnhancer)
	);

	return store;
}
