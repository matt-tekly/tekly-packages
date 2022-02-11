import { RouteComponentProps } from 'react-router';

import { IMenuItemProps } from '@blueprintjs/core';

import ApiView from '~app/api/components/ApiView';
import AssetsNavBar from '~app/assets/components/AssetsNavbar';
import AssetsView from '~app/assets/components/AssetsView';
import { CUSTOM_APP_ROUTES } from '~app/custom/CustomAppRoutes';
import DiskView from '~app/disk/components/DiskView';
import DiskNavBar from '~app/disk/components/DiskNavBar';
import FramelineNavbar from '~app/frameline/components/FramelineNavbar';
import FramelineView from '~app/frameline/components/FramelineView';
import GameObjectsNavBar from '~app/gameobject/components/GameObjectsNavBar';
import GameObjectsView from '~app/gameobject/components/GameObjectsView';
import HomeView from '~app/home/components/HomeView';
import HomeNavbar from '~app/home/components/HomeNavbar';

export type IAppRouteComponent = React.ComponentType<RouteComponentProps<any>> | React.ComponentType<any>;

export interface IAppRoute {
	mainComponent: IAppRouteComponent;
	navBarComponent?: IAppRouteComponent;
	navPanelProps?: IMenuItemProps & React.AnchorHTMLAttributes<HTMLAnchorElement>;
	route: string;
}

export const DEFAULT_APP_ROUTES: IAppRoute[] = [
	{
		route: '/',
		mainComponent: HomeView,
		navBarComponent: HomeNavbar,
		navPanelProps: {
			icon: 'home',
			text: 'Home'
		}
	},
	{
		route: '/assets',
		mainComponent: AssetsView,
		navBarComponent: AssetsNavBar,
		navPanelProps: {
			icon: 'console',
			text: 'Assets'
		}
	},
	{
		route: '/commands',
		mainComponent: ApiView,
		navPanelProps: {
			icon: 'console',
			text: 'Commands'
		}
	},
	{
		route: '/disk',
		mainComponent: DiskView,
		navBarComponent: DiskNavBar,
		navPanelProps: {
			icon: 'folder-open',
			text: 'Disk'
		}
	},
	{
		route: '/frameline',
		navBarComponent: FramelineNavbar,
		mainComponent: FramelineView,
		navPanelProps: {
			icon: 'gantt-chart',
			text: 'Frameline'
		}
	},
	{
		route: '/gameobjects',
		mainComponent: GameObjectsView,
		navBarComponent: GameObjectsNavBar,
		navPanelProps: {
			icon: 'diagram-tree',
			text: 'GameObjects'
		}
	}
];

export const ALL_ROUTES = DEFAULT_APP_ROUTES
							.concat(CUSTOM_APP_ROUTES)
							.sort((x, y) => x.route.localeCompare(y.route));
