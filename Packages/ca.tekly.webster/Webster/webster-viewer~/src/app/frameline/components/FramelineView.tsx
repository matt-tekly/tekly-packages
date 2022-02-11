import * as React from 'react';
import { connect } from 'react-redux';

import { IAppState } from '~app/AppState';
import { IFramelineAppConfig, IFramelineData } from '../FramelineTypes';
import { setData } from '../reducers/FramelineActions';
import { FramelineTable } from './table/FramelineTable';
import FramelineTimeline from './timeline/FramelineTimeline';



import { GameFetcher } from '~app/utility/Fetcher';

interface IFramelineViewProps {
	data: IFramelineData;
	config: IFramelineAppConfig;
	setData: (data: IFramelineData) => void;
}

class FramelineView extends React.Component<IFramelineViewProps, {}> {

	public render() {
		if (this.props.config.viewMode === 'timeline') {
			return <FramelineTimeline />;
		}

		return <FramelineTable data={this.props.data}/>;
	}

	public componentDidMount() {
		this.doFetchFrameline();
	}

	private doFetchFrameline = async () => {
		const resp = await GameFetcher.get('api/frameline', 'Json');
		const data = resp.body as IFramelineData;

		this.props.setData(data);
	}
}

const mapStateToProps = (state: IAppState) => ({
	data: state.frameline.data,
	config: state.frameline.config
});

export default connect(mapStateToProps, { setData })(FramelineView);
