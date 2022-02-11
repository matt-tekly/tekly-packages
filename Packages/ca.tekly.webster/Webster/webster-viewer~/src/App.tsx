import * as React from 'react';
import { BrowserRouter as Router, Route } from 'react-router-dom';

import { Alignment, Button, FocusStyleManager, Navbar, Position, Tooltip } from '@blueprintjs/core';

import { ALL_ROUTES, IAppRoute, IAppRouteComponent } from '~AppRoutes';
import NavPanel from '~NavPanel';

import * as AppPrefs from '~app/utility/AppPrefs';
import { FileUploaderView } from '~app/disk/components/FileUploaderView';

FocusStyleManager.onlyShowFocusOnTabs();

interface IAppState {
	showNavPanel: boolean;
	showNavigationToolTip: boolean;
}

const SHOW_NAVIGATION_TOOLTIP_KEY = 'SHOW_NAVIGATION_TOOLTIP_KEY';

export default class App extends React.Component<{}, IAppState> {

	constructor(props: {}) {
		super(props);
		this.state = {
			showNavPanel: false,
			showNavigationToolTip: AppPrefs.getBoolean(SHOW_NAVIGATION_TOOLTIP_KEY, true),
		};
	}

	public render() {
		const navRoutes = ALL_ROUTES.filter(x => !!x.navBarComponent).map(route => this.renderRoute(route, route.navBarComponent));
		const mainRoutes = ALL_ROUTES.map(route => this.renderRoute(route, route.mainComponent));

		return (
			<div className='app-container bp3-dark'>
				<FileUploaderView />
				<Router>
					<Navbar>
						<Navbar.Group align={Alignment.LEFT}>
							<Tooltip content='Click here to navigate' position={Position.BOTTOM_RIGHT} isOpen={this.state.showNavigationToolTip}>
								<Button text='Webster' icon='menu' minimal large onClick={this.handleOpen} />
							</Tooltip>
							<Navbar.Divider />
						</Navbar.Group>
						{navRoutes}
					</Navbar>
					<div className='app-content'>
						{mainRoutes}
					</div>
					<NavPanel showNavPanel={this.state.showNavPanel} hidePanel={this.handleClose} />
				</Router>
			</div>
		);
	}

	private handleOpen = () => {
		AppPrefs.setBoolean(SHOW_NAVIGATION_TOOLTIP_KEY, false);
		this.setState({ showNavPanel: true, showNavigationToolTip: false });
	}

	private handleClose = () => this.setState({ showNavPanel: false });

	private renderRoute = (appRoute: IAppRoute, component?: IAppRouteComponent) => {
		return <Route path={appRoute.route} exact component={component} key={appRoute.route} />;
	}
}
