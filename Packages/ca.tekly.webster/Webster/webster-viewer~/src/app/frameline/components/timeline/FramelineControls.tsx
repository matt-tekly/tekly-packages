import * as React from 'react';
import { connect } from 'react-redux';

import { Button, ButtonGroup, Card, Classes, Elevation, FormGroup, H5, Icon, NumericInput } from '@blueprintjs/core';

import { IFramelineAppConfig, IFramelineData, IFramelineEvent, IFramelineEventConfig } from '../../FramelineTypes';

import { setConfigOption, setData } from '../../reducers/FramelineActions';

import { GameFetcher } from '~/app/utility/Fetcher';
import { IAppState } from '~app/AppState';
import { getEventConfig } from '../../reducers/FramelineReducer';

interface IFramelineControlsProps {
	framelineAppConfig: Readonly<IFramelineAppConfig>;
	selectedEvent?: IFramelineEvent;
	selectedEventConfig: IFramelineEventConfig;
	setConfigOption: (payload: Partial<IFramelineAppConfig>) => void;
	setData: (payload: IFramelineData) => void;
}

class FramelineControls extends React.Component<IFramelineControlsProps> {

	public render() {
		const pixelsPerSecond = this.props.framelineAppConfig.pixelsPerSecond;
		const minEventTime = this.props.framelineAppConfig.minEventTime;

		return (
			<div className={`fl-ui ${Classes.DRAWER_BODY}`}>
				<Card elevation={Elevation.TWO}>
					<H5>
						<a href='#'>Settings</a>
					</H5>
					<FormGroup label='Pixels Per Second' labelFor='pps-input'>
						<NumericInput
							id='pps-input'
							intent='success'
							value={pixelsPerSecond}
							onValueChange={this.handlePixelsPerSecondChange}
							min={10}
							clampValueOnBlur={true}
						/>
					</FormGroup>

					<FormGroup label='Min Event time' labelFor='met-input'>
						<NumericInput
							id='met-input'
							intent='success'
							value={minEventTime}
							onValueChange={this.handleMinEventTime}
							min={10}
							clampValueOnBlur={true}
						/>
					</FormGroup>
				</Card>
				<Card elevation={Elevation.TWO}>
					{this.renderSelectedEvent()}
				</Card>
			</div>
		);
	}

	private handlePixelsPerSecondChange = (valueAsNumber: number) => {
		this.props.setConfigOption({ pixelsPerSecond: valueAsNumber });
	}

	private handleMinEventTime = (valueAsNumber: number) => {
		this.props.setConfigOption({ minEventTime: valueAsNumber });
	}

	private renderSelectedEvent = () => {
		if (!this.props.selectedEvent) {
			return (
				<H5>
					<a href='#'>No Selection</a>
				</H5>
			);
		}

		const evt = this.props.selectedEvent;
		const evtConfig = this.props.selectedEventConfig;
		const duration = (evt.EndTime - evt.StartTime) / 1000;
		const startTime = evt.StartTime / 1000;
		const endTime = evt.EndTime / 1000;

		return (
			<div>
				<div className='event-config-container'>
					<H5>
						<a href='#'>{evt.EventType}</a>
					</H5>
					<div className='event-config-color-box' style={{ background: evtConfig.Color, margin: '3px 0 0 0' }} />
				</div>
				<div>
					<div>{`${evt.Id}`}</div>
					<p> <Icon icon='time' /> {` ${duration.toFixed(4)}s`}</p>
					<p> {`${startTime.toFixed(4)} - ${endTime.toFixed(4)}`}</p>
					<ButtonGroup>
						<Button text='Clear Before' onClick={this.clearToSelected} />
						<Button text='Clear After' onClick={this.clearAfterSelected} />
					</ButtonGroup>
				</div>
			</div>
		);
	}

	private fetchFrameline = async () => {
		const resp = await GameFetcher.get('api/frameline', 'Json');
		const data = resp.body as IFramelineData;

		this.props.setData(data);
	}

	private clearToSelected = async () => {
		if (!this.props.selectedEvent) {
			return;
		}

		const evt = this.props.selectedEvent;

		await GameFetcher.delete(`api/frameline/clearTo?time=${evt.StartTime}`);
		await this.fetchFrameline();
	}

	private clearAfterSelected = async () => {
		if (!this.props.selectedEvent) {
			return;
		}

		const evt = this.props.selectedEvent;

		await GameFetcher.delete(`api/frameline/clearAfter?time=${evt.EndTime}`);
		await this.fetchFrameline();
	}
}

const mapStateToProps = (state: IAppState) => {

	const { config, selectedEvent } = state.frameline;

	const eventId = selectedEvent ? selectedEvent.EventType : 'General';

	return {
		framelineAppConfig: config,
		selectedEvent,
		selectedEventConfig: getEventConfig(eventId, state.frameline.data.Config)
	};
};

export default connect(mapStateToProps, { setConfigOption, setData })(FramelineControls);
