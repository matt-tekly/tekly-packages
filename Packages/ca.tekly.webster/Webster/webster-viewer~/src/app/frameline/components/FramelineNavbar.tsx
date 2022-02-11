import * as React from 'react';
import { connect } from 'react-redux';

import { Alignment, Button, ButtonGroup, FileInput, Navbar, NavbarDivider, HotkeysTarget, Hotkeys, Hotkey } from '@blueprintjs/core';

import { IAppState } from '~app/AppState';
import { GameFetcher } from '~app/utility/Fetcher';
import { IFramelineAppConfig, IFramelineData } from '../FramelineTypes';
import { setConfigOption, setData } from '../reducers/FramelineActions';

interface IFramelineNavbarState {
	fileText: string;
}

interface IFramelineNavbarProps {
	data: IFramelineData;
	config: IFramelineAppConfig;
	setData: (data: IFramelineData) => void;
	setConfigOption: (config: Partial<IFramelineAppConfig>) => void;
}

const inputProps = {
	accept: '.json',
};

@HotkeysTarget
class FramelineNavbar extends React.Component<IFramelineNavbarProps, IFramelineNavbarState> {

	public state = {
		fileText: 'Upload data'
	};

	public render() {

		const {config} = this.props;

		const timeline = config.viewMode === 'timeline';
		const table = config.viewMode === 'table';

		return (
			<div>
				<Navbar.Group align={Alignment.LEFT}>
					<Navbar.Heading>Frameline Viewer</Navbar.Heading>
				</Navbar.Group>

				<Navbar.Group align={Alignment.RIGHT}>
					<ButtonGroup>
						<Button text='Timeline' active={timeline} onClick={this.setViewModeTimeline} intent='primary' />
						<Button text='Table' active={table} onClick={this.setViewModeTable} intent='primary' />
					</ButtonGroup>
					<NavbarDivider />
					<FileInput text={this.state.fileText} onInputChange={this.onFileChange} inputProps={inputProps} />
					<Button className='bp3-minimal' icon='floppy-disk' text='Save' onClick={this.saveData} />
					<Button className='bp3-minimal' icon='refresh' text='Refresh' onClick={this.fetchFrameline} />
					<Button className='bp3-minimal' icon='delete' text='Clear' onClick={this.clearFrameline} />
				</Navbar.Group>
			</div>
		);
	}

	public renderHotkeys() {
		return (
			<Hotkeys>
				<Hotkey global={true} combo='r' label='Refresh Frameline' onKeyDown={this.fetchFrameline} />
			</Hotkeys>
		);
	}

	private setViewModeTimeline = () => this.props.setConfigOption({viewMode: 'timeline'});
	private setViewModeTable = () => this.props.setConfigOption({viewMode: 'table'});

	private onFileChange = (inputEvent: React.FormEvent<HTMLInputElement>) => {
		if (inputEvent.currentTarget.files) {
			const file = inputEvent.currentTarget.files[0];
			this.setState({ fileText: file.name });

			const reader = new FileReader();
			reader.onload = () => {
				const json = reader.result as string;
				try {
					const obj = JSON.parse(json);
					this.props.setData(obj as IFramelineData);
				} catch (err) {
					console.error(`Failed to parse FramelineData JSON \n${err}`);
				}
			};
			reader.readAsText(file);
		}
	}

	private fetchFrameline = async () => {
		this.setState({ fileText: 'Upload data' });
		this.doFetchFrameline();
	}

	private clearFrameline = async () => {
		this.setState({ fileText: 'Upload data' });
		this.doClearFrameline();
	}

	private saveData = () => {
		const json = JSON.stringify(this.props.data, null, 4);
		const dateString = this.getDateString(new Date());

		this.saveText(json, `frameline_${dateString}.json`);
	}

	private getDateString = (date: Date) => {
		let year = date.getFullYear();
		let month = (date.getMonth() + 1).toString().padStart(2, '0');
		let day = date.getDate().toString().padStart(2, '0');
		let hour = date.getHours().toString().padStart(2, '0');
		let minute = date.getMinutes().toString().padStart(2, '0');
		let second = date.getSeconds().toString().padStart(2, '0');

		return `${year}-${month}-${day}_${hour}_${minute}_${second}`;
	}

	private saveText(text: string, filename: string) {
		const a = document.createElement('a');
		document.body.appendChild(a);

		a.setAttribute('href', 'data:text/plain;charset=utf-u,' + encodeURIComponent(text));
		a.setAttribute('download', filename);
		a.click();

		document.body.removeChild(a);
	}

	private doFetchFrameline = async () => {
		const resp = await GameFetcher.get('api/frameline', 'Json');
		const data = resp.body as IFramelineData;

		this.props.setData(data);
	}

	private doClearFrameline = async () => {
		await GameFetcher.delete('api/frameline');
		await this.fetchFrameline();
	}
}

const mapStateToProps = (state: IAppState) => {
	return {
		data: state.frameline.data,
		config: state.frameline.config
	};
};

export default connect(mapStateToProps, { setData, setConfigOption })(FramelineNavbar);
