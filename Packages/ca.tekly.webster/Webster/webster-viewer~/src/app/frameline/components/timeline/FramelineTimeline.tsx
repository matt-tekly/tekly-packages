import * as React from 'react';
import { connect } from 'react-redux';

import { Switch } from '@blueprintjs/core';

import { IAppState } from '~app/AppState';
import { IFramelineAppConfig, IFramelineData, IFramelineEventConfig } from '../../FramelineTypes';
import { enableEvent, IEnableEventPayload } from '../../reducers/FramelineActions';
import FramelineCanvas from '../canvas/FramelineCanvas';
import FramelineControls from './FramelineControls';

interface IFramelineViewProps {
	data: IFramelineData;
	config: IFramelineAppConfig;
	eventConfigs: IFramelineEventConfig[];
	enableEvent: (payload: IEnableEventPayload) => void;
}

class FramelineTimeline extends React.Component<IFramelineViewProps, {}> {

	public render() {
		return (
			<div className='frameline-container'>
				<FramelineControls />
				<div className='fl-canvas-area'>
					<div className='fl-canvas-area-left'>
						<p>Frameline Event Types</p>
						{this.renderEventTypes()}
					</div>
					<FramelineCanvas />
				</div>
			</div>
		);
	}

	private renderEventTypes = () => {
		const evtConfigs = this.props.eventConfigs;

		return evtConfigs.map(evt => {
			return this.renderEventConfig(evt);
		});
	}

	private renderEventConfig = (evt: IFramelineEventConfig) => {
		const enabled = this.props.config.disabledEvents.indexOf(evt.Id) === -1;

		const handleChange = (event: React.FormEvent<HTMLInputElement>): void => {
			this.props.enableEvent({ eventId: evt.Id, enabled: event.currentTarget.checked });
		};

		return (
			<div className='event-config-container' key={evt.Id}>
				<Switch label={evt.Id} checked={enabled} onChange={handleChange} />
				{this.renderBox(evt.Color)}
			</div>
		);
	}

	private renderBox = (color: string) => {
		return (
			<div className='event-config-color-box' style={{ background: color }} />
		);
	}
}

const mapStateToProps = (state: IAppState) => ({
	data: state.frameline.data,
	config: state.frameline.config,
	eventConfigs: state.frameline.eventTypes
});

export default connect(mapStateToProps, { enableEvent })(FramelineTimeline);
