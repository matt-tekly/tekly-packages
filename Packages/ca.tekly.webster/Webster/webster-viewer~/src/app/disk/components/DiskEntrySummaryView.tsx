import * as React from 'react';
import { connect } from 'react-redux';

import { Alignment, Button, ButtonGroup, IconName, Intent, Navbar, Popover, Position, Tooltip  } from '@blueprintjs/core';

import { IAppState } from '~app/AppState';
import IconWithText from '~app/components/IconWithText';
import { GameFetcher } from '~app/utility/Fetcher';
import { humanFileSize } from '~app/utility/HumanFileSize';
import { IDiskEntrySummary } from '../DiskTypes';
import { deleteEntry } from '../reducers/DiskActions';
import Fileview from './FileView';

interface IDiskEntrySummaryViewProps {
	entry?: IDiskEntrySummary;
	deleteEntry: (path: string) => void;
}

class DiskEntrySummaryView extends React.Component<IDiskEntrySummaryViewProps> {

	public render() {
		return (
			<div className='file-summary-container'>
				<div className='file-summary-header'>
					{this.renderEntryName()}
					{this.renderButtons()}
				</div>
				<Fileview selectedEntry={this.props.entry} />
			</div>
		);
	}

	private renderEntryName() {
		const info = this.getDetailedInfo();
		return (
			<div>
				<div>
					{this.renderIconAndName()}
				</div>
				<div className='bp3-text-muted bp3-text-small'>
					<span>{info.size}</span>
					<span>{' - - '}</span>
					<span>{info.lastModified}</span>
				</div>
			</div>
		);
	}

	private getDetailedInfo() {
		const info = {
			size: '',
			lastModified: ''
		};

		if (this.props.entry && this.props.entry.Type === 'File') {
			info.size = humanFileSize(this.props.entry.Size);
			info.lastModified = this.props.entry.LastWriteTime;
		}
		return info;
	}

	private renderIconAndName() {
		let icon: IconName = 'info-sign';
		let text: string = 'No Selection';
		if (this.props.entry) {
			icon = this.props.entry.Type === 'Directory' ? 'folder-open' : 'document';
			text = this.props.entry.Name;
		}

		return <IconWithText icon={icon} text={text} />;
	}

	private renderButtons() {
		return (
			<Navbar.Group align={Alignment.RIGHT}>
				<ButtonGroup>
					<Popover position={Position.TOP}>
						<Tooltip content='Download' position={Position.TOP} hoverOpenDelay={500}>
							<Button icon='download' onClick={this.downloadEntry} intent={Intent.PRIMARY} />
						</Tooltip>
					</Popover>
					<Popover position={Position.TOP}>
						<Tooltip content='Delete' position={Position.TOP} hoverOpenDelay={500}>
							<Button icon='delete' onClick={this.deleteEntry} intent={Intent.DANGER} />
						</Tooltip>
					</Popover>
				</ButtonGroup>
			</Navbar.Group>
		);
	}

	private downloadEntry = () => {
		if (!this.props.entry) {
			return;
		}

		const downloadPath = `api/disk${this.props.entry.Path}`;
		window.open(downloadPath);
	}

	private deleteEntry = async () => {
		if (!this.props.entry) {
			return;
		}

		this.props.deleteEntry(this.props.entry.Path);
	}
}

const mapStateToProps = (state: IAppState) => ({
	entry: state.disk.entryMap[state.disk.selectedEntry]
});

export default connect(mapStateToProps, { deleteEntry })(DiskEntrySummaryView);
