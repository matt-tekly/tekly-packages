import * as React from 'react';
import { connect } from 'react-redux';

import { Alignment, Button, Navbar, HotkeysTarget, Hotkeys, Hotkey } from '@blueprintjs/core';

import { IAppState } from '~app/AppState';
import { refreshAssets } from '../reducers/AssetsActions';

interface IProps {
	refreshAssets: () => void;
}

@HotkeysTarget
class AssetsNavbar extends React.Component<IProps> {

	public render() {
		return (
			<div>
				<Navbar.Group align={Alignment.LEFT}>
					<Navbar.Heading>Assets</Navbar.Heading>
				</Navbar.Group>

				<Navbar.Group align={Alignment.RIGHT}>
					<Button minimal icon='refresh' text='Refresh' onClick={this.refreshAssets} />
				</Navbar.Group>
			</div>
		);
	}

	private refreshAssets = () => {
		this.props.refreshAssets();
	}

	public renderHotkeys() {
		return (
			<Hotkeys>
				<Hotkey global={true} combo='r' label='Refresh Assets' onKeyDown={this.props.refreshAssets} />
			</Hotkeys>
		);
	}

}

const mapStateToProps = (state: IAppState) => ({});

export default connect(mapStateToProps, { refreshAssets })(AssetsNavbar);
