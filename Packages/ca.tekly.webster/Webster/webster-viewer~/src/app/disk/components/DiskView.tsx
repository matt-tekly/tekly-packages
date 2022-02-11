import * as React from 'react';
import { connect } from 'react-redux';

import { IAppState } from '~app/AppState';
import { refreshDirectories, setFilter, setSelectedEntry } from '../reducers/DiskActions';

import { IDirectorySummary, IDiskEntrySummary } from '../DiskTypes';
import DiskEntriesView from './DiskEntriesView';
import DiskEntrySummaryView from './DiskEntrySummaryView';

interface IDiskViewDispatchProps {
	refreshDirectories: () => void;
	setSelectedEntry: (entry: IDiskEntrySummary) => void;
	setFilter: (filter: string) => void;
}

interface IDiskViewProps {
	directory: IDirectorySummary;
	selectedEntry?: IDiskEntrySummary;
	filter: string;
}

class DiskView extends React.Component<IDiskViewProps & IDiskViewDispatchProps> {

	public render() {
		const { directory, filter, selectedEntry } = this.props;
		const selected = selectedEntry ? selectedEntry.Path : '';

		return (
			<div className='disk-view'>
				<DiskEntriesView
					directory={directory}
					filter={filter}
					selected={selected}
					setFilter={this.props.setFilter}
					onEntrySelected={this.props.setSelectedEntry}
				/>
				<DiskEntrySummaryView />
			</div>
		);
	}

	public componentDidMount() {
		this.refreshDirectorySummary();
	}

	private refreshDirectorySummary = () => {
		this.props.refreshDirectories();
	}
}

const mapStateToProps = (state: IAppState): IDiskViewProps => ({
	directory: state.disk.directory,
	selectedEntry: state.disk.entryMap[state.disk.selectedEntry],
	filter: state.disk.filterText
});

export default connect(mapStateToProps, { refreshDirectories, setSelectedEntry, setFilter })(DiskView);
