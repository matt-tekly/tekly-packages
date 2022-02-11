import * as React from 'react';
import { RouteComponentProps, withRouter } from 'react-router-dom';

import { Classes, Drawer, Menu, MenuDivider, MenuItem } from '@blueprintjs/core';
import { ALL_ROUTES, IAppRoute } from '~AppRoutes';

export interface INavPanelProps extends RouteComponentProps {
	showNavPanel: boolean;
	hidePanel: () => void;
}

interface INavRoute {
	route: IAppRoute;
	onClick: (event: React.MouseEvent<HTMLElement>) => void;
}

class NavPanel extends React.Component<INavPanelProps, {}> {

	private navRoutes: INavRoute[];

	constructor(props: INavPanelProps) {
		super(props);
		this.navRoutes = ALL_ROUTES.map(this.createHandler);
	}

	public render() {
		const navRouteMenuItems = this.navRoutes.map(this.renderMenuItem);

		return (
			<Drawer onClose={this.handleClose}
				title='Navigation'
				isOpen={this.props.showNavPanel}
				position='left'
				size={Drawer.SIZE_SMALL}
				className={Classes.DARK}
			>
				<Menu className={Classes.ELEVATION_1}>
					<MenuDivider title='Main' />
					{navRouteMenuItems}
					<MenuItem
						icon='camera'
						text='Screen Shot'
						href='screenshot'
						target='_blank' rel='noopener noreferrer'
					/>
				</Menu>
			</Drawer>
		);
	}

	private renderMenuItem = (navRoute: INavRoute, index: number) => {
		return <MenuItem {...navRoute.route.navPanelProps} onClick={navRoute.onClick} key={index} />;
	}

	private createHandler = (route: IAppRoute) => {
		return {
			route,
			onClick: () => {
				this.props.history.push(route.route);
				this.props.hidePanel();
			}
		};
	}

	private handleClose = () => this.props.hidePanel();
}

export default withRouter(NavPanel);
