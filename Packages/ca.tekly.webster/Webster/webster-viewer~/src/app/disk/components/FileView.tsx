import * as React from 'react';

import SelectableTextArea from '~app/components/SelectableTextArea';
import { GameFetcher } from '~app/utility/Fetcher';
import { humanFileSize } from '~app/utility/HumanFileSize';
import { IDiskEntrySummary, IFileSummary } from '../DiskTypes';

interface IFileViewState {
	path: string;
	data: string;
	isImage: boolean;
}

interface IFileViewProps {
	selectedEntry?: IDiskEntrySummary;
}

const LARGE_FILE = 1024 * 1024;
const IMAGE_EXTENSIONS = ['png', 'bmp', 'jpg', 'jpeg', 'gif', 'svg'];

export default class Fileview extends React.Component<IFileViewProps, IFileViewState> {

	public state: IFileViewState = {
		path: '',
		data: '',
		isImage: false
	};

	public render() {
		if (!this.props.selectedEntry || this.props.selectedEntry.Type === 'Directory') {
			return (
				<div className='file-view-container'>
					<SelectableTextArea className='data-text' value='No File Selected' />
				</div>
			);
		}

		if (this.state.isImage) {
			const path = `api/disk${this.props.selectedEntry.Path}`;
			return (
				<div className='file-view-container'>
					<img className='file-view-image' src={path} />
				</div>
			)
		}

		return (
			<div className='file-view-container'>
				<SelectableTextArea className='data-text' value={this.state.data} />
			</div>
		);
	}

	public componentDidUpdate(prevProps: IFileViewProps) {
		if (this.props.selectedEntry !== prevProps.selectedEntry) {

			if (this.props.selectedEntry && this.props.selectedEntry.Type === 'File') {
				const isImage = this.isImage(this.fileExtension(this.props.selectedEntry.Path));
				if (!isImage) {
					this.fetchFile(this.props.selectedEntry);;
				}

				this.setState({ isImage });

			} else {
				this.setState({ data: '', isImage: false });
			}
		}
	}

	private async fetchFile(fileEntry: IFileSummary) {
		if (fileEntry.Size >= LARGE_FILE) {
			this.setState({ data: `File is over maximum display size [${humanFileSize(LARGE_FILE)}]` });
			return;
		}

		const downloadPath = `api/disk${fileEntry.Path}`;
		const resp = await GameFetcher.get(downloadPath, 'Text');

		// Ensure that we don't overwrite the data if the selected file has changed
		// since we requested the file
		if (this.props.selectedEntry !== fileEntry) {
			return;
		}

		if (!resp.response.ok) {
			this.setState({ data: '' });
			return;
		}

		this.setState({ data: this.formatString(resp.body, fileEntry) });
	}

	private formatString(text: string, fileEntry: IFileSummary) {
		const ext = this.fileExtension(fileEntry.Name);

		if (ext === 'json') {
			return JSON.stringify(JSON.parse(text), null, 4);
		}

		return text;
	}

	private fileExtension(fileName: string) {
		return fileName.slice((Math.max(0, fileName.lastIndexOf('.')) || Infinity) + 1).toLowerCase();
	}

	private isImage(fileName: string) {
		return IMAGE_EXTENSIONS.includes(fileName.toLowerCase());
	}
}
