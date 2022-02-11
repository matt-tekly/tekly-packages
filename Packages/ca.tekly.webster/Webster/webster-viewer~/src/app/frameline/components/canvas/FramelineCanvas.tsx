import * as React from 'react';
import { connect } from 'react-redux';

import { IResizeEntry, ResizeSensor } from '@blueprintjs/core';

import { IAppState } from '~app/AppState';
import { IFramelineAppConfig, IFramelineData, IFramelineEvent } from '../../FramelineTypes';
import { setSelectedEvent } from '../../reducers/FramelineActions';

import { CanvasController } from './CanvasController';

interface IFramelineCanvasProps {
	data: IFramelineData;
	config: IFramelineAppConfig;
	setSelectedEvent: (event?: IFramelineEvent) => void;
}

interface IState {
	canvasWidth: number;
}

class FramelineCanvas extends React.Component<IFramelineCanvasProps, IState> {

	private scrollContainer = React.createRef<HTMLDivElement>();
	private viewContainer = React.createRef<HTMLDivElement>();
	private canvasContainer = React.createRef<HTMLDivElement>();

	private canvasController?: CanvasController;

	public state: IState = {
		canvasWidth: 1000
	};

	public componentDidMount() {
		if (!this.canvasContainer.current || !this.viewContainer.current || !this.scrollContainer.current) {
			console.error('Failed to find element to bind canvas to.');
			return;
		}

		this.canvasController = new CanvasController(this.canvasContainer.current, this.props.setSelectedEvent);

		this.scrollContainer.current.addEventListener('scroll', () => {
			if (this.canvasController && this.scrollContainer.current) {
				this.canvasController.onScroll(this.scrollContainer.current);
			}
		});

		const canvasWidth = this.canvasController.render(this.props.data, this.props.data.Config, this.props.config);
		this.setState({canvasWidth});
	}

	public componentDidUpdate(prevProps: IFramelineCanvasProps) {
		if (prevProps.config === this.props.config && prevProps.data === this.props.data) {
			return;
		}

		if (this.canvasController) {
			const canvasWidth = this.canvasController.render(this.props.data, this.props.data.Config, this.props.config);
			this.setState({canvasWidth});
		}
	}

	public render() {
		const style = {
			width: (this.state.canvasWidth + 60) + 'px',
		};

		return (
			<ResizeSensor onResize={this.onResizeSensor}>
				<div className='fl-canvas' ref={this.scrollContainer}>
					<div className='fl-viewport' ref={this.viewContainer} style={style}>
						<div ref={this.canvasContainer}></div>
					</div>
				</div>
			</ResizeSensor>
		);
	}

	private onResizeSensor = (entries: IResizeEntry[]) => {
		if (this.canvasController) {
			this.canvasController.onResizeSensor(entries[0].contentRect);
		}
	}
}

const mapStateToProps = (state: IAppState) => ({
	data: state.frameline.data,
	config: state.frameline.config
});

export default connect(mapStateToProps, { setSelectedEvent })(FramelineCanvas);
