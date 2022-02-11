import * as React from 'react';
import { connect } from 'react-redux';

import { Alignment, Button, Navbar, HotkeysTarget, Hotkeys, Hotkey } from '@blueprintjs/core';

import { IAppState } from '~app/AppState';
import { refreshDirectories } from '../reducers/DiskActions';

interface IProps {
	refreshDirectories: () => void;
}

@HotkeysTarget
class DiskNavBar extends React.Component<IProps> {

	public render() {
		return (
			<div>
				<Navbar.Group align={Alignment.LEFT}>
					<Navbar.Heading>Disk</Navbar.Heading>
				</Navbar.Group>

				<Navbar.Group align={Alignment.RIGHT}>
					<Button minimal icon='refresh' text='Refresh' onClick={this.refreshDirectories} />
				</Navbar.Group>
			</div>
		);
	}

	private refreshDirectories = () => {
		this.props.refreshDirectories();
	}

	public renderHotkeys() {
		return (
			<Hotkeys>
				<Hotkey global={true} combo='r' label='Refresh Disk' onKeyDown={this.refreshDirectories} />
			</Hotkeys>
		);
	}

}

const mapStateToProps = (state: IAppState) => ({});

export default connect(mapStateToProps, { refreshDirectories })(DiskNavBar);
