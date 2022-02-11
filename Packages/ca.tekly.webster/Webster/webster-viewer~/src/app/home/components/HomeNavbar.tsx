import * as React from 'react';

import { Alignment, Navbar, HotkeysTarget, Hotkeys, Hotkey, Button } from '@blueprintjs/core';
import { refreshInfoSummary } from '../reducers/HomeActions';
import { IAppState } from '~app/AppState';
import { connect } from 'react-redux';

interface IProps {
	refreshInfoSummary: () => void;
}

@HotkeysTarget
class HomeNavbar extends React.Component<IProps> {
	public render() {
		return (
			<div>
				<Navbar.Group align={Alignment.LEFT}>
					<Navbar.Heading>Home</Navbar.Heading>
				</Navbar.Group>

				<Navbar.Group align={Alignment.RIGHT}>
					<Button minimal icon='refresh' text='Refresh' onClick={this.refreshInfoSummary} />
				</Navbar.Group>
			</div>
		);
	}

	private refreshInfoSummary = () => {
		this.props.refreshInfoSummary();
	}

	public renderHotkeys() {
		return (
			<Hotkeys>
				<Hotkey global={true} combo='r' label='Refresh Info summary' onKeyDown={this.props.refreshInfoSummary} />
			</Hotkeys>
		);
	}
}

const mapStateToProps = (state: IAppState) => ({});

export default connect(mapStateToProps, { refreshInfoSummary })(HomeNavbar);