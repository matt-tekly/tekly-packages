import { IApiState } from './api/reducers/ApiReducer';
import { IAssetsState } from './assets/reducers/AssetsReducer';
import { ICustomState } from './custom/reducers/customReducer';
import { IDiskState } from './disk/reducers/DiskReducer';
import { IFramelineState } from './frameline/reducers/FramelineReducer';
import { IGameObjectState } from './gameobject/reducers/GameObjectReducer';
import { IHomeState } from './home/reducers/HomeReducer';

export interface IAppState {
	home: IHomeState
	assets: IAssetsState;
	api: IApiState;
	disk: IDiskState;
	frameline: IFramelineState;
	gameObject: IGameObjectState;
	custom: ICustomState;
}
