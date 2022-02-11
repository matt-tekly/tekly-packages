import * as React from 'react';
import { connect } from 'react-redux';

import { Alignment, Button, Navbar, Hotkeys, Hotkey, HotkeysTarget } from '@blueprintjs/core';

import { IAppState } from '~app/AppState';
import { refreshHierarchy } from '../reducers/GameObjectActions';

interface IProps {
	refreshHierarchy: () => void;
}

@HotkeysTarget
class GameObjectsNavBar extends React.Component<IProps> {

	public render() {
		return (
			<div>
				<Navbar.Group align={Alignment.LEFT}>
					<Navbar.Heading>Game Objects</Navbar.Heading>
				</Navbar.Group>

				<Navbar.Group align={Alignment.RIGHT}>
					<Button minimal icon='refresh' text='Refresh' onClick={this.refreshHierarchy} />
				</Navbar.Group>
			</div>
		);
	}

	public renderHotkeys() {
		return (
			<Hotkeys>
				<Hotkey global={true} combo='r' label='Refresh GameObjects' onKeyDown={this.props.refreshHierarchy} />
			</Hotkeys>
		);
	}

	private refreshHierarchy = () => {
		this.props.refreshHierarchy();
	}

}

const mapStateToProps = (state: IAppState) => ({});

export default connect(mapStateToProps, { refreshHierarchy })(GameObjectsNavBar);
