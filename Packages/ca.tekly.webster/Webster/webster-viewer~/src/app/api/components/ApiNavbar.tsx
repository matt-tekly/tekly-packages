import * as React from 'react';

import { Alignment, Navbar } from '@blueprintjs/core';

export default class ApiNavbar extends React.Component<{}, {}> {
	public render() {
		return (
			<div>
				<Navbar.Group align={Alignment.LEFT}>
					<Navbar.Heading>Commands</Navbar.Heading>
				</Navbar.Group>
			</div>
		);
	}
}
