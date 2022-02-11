import * as React from 'react';

import SearchTree from '~app/components/trees/SearchTree';
import { ITeklyTreeNode } from '~app/components/trees/TeklyTreeTypes';
import { IDirectorySummary, IDiskEntrySummary, IFileSummary } from '../DiskTypes';
import { FileUploaderView } from './FileUploaderView';

interface IDiskEntriesViewProps {
	directory: IDirectorySummary;
	filter: string;
	selected: string;
	setFilter: (filter: string) => void;
	onEntrySelected: (selected: IDiskEntrySummary) => void;
}

interface IDiskEntriesViewState {
	nodes: Array<ITeklyTreeNode<IDiskEntrySummary>>;
	expandedNodes: string[];
	selectedNode: string;
}

export default class DiskEntriesView extends React.Component<IDiskEntriesViewProps, IDiskEntriesViewState> {

	constructor(props: IDiskEntriesViewProps) {
		super(props);

		this.state = {
			nodes: this.toTreeNodes(this.props.directory),
			selectedNode: '',
			expandedNodes: []
		};
	}

	public componentDidUpdate(prevProps: IDiskEntriesViewProps) {
		if (prevProps.directory !== this.props.directory) {
			this.setState({
				nodes: this.toTreeNodes(this.props.directory)
			});
		}
	}

	public render() {
		return (
			<div className='round-container'>
				<SearchTree
					placeholder='Search Files'
					expandedNodes={this.state.expandedNodes}
					selectedNode={this.state.selectedNode}
					onNodesExpanded={this.onNodesExpanded}
					onNodeSelected={this.onNodeSelected}
					nodes={this.state.nodes}
					onNodeDrag={this.onNodeDrag}
					onNodeDrop={this.onNodeDrop}
				/>
			</div>
		);
	}

	private onNodesExpanded = (expandedNodes: string[]) => {
		this.setState({expandedNodes});
	}

	private onNodeSelected = (node: ITeklyTreeNode<IDiskEntrySummary>) => {
		this.setState({selectedNode: node.id});
		if (node.nodeData) {
			this.props.onEntrySelected(node.nodeData);
		}
	}

	private toTreeNodes = (directory: IDirectorySummary): Array<ITeklyTreeNode<IDiskEntrySummary>> => {
		return this.convertDirectoryToNode(directory).children || [];
	}

	private convertDirectoryToNode = (directory: IDirectorySummary): ITeklyTreeNode<IDiskEntrySummary> => {
		const children: Array<ITeklyTreeNode<IDiskEntrySummary>> = [];

		const node: ITeklyTreeNode<IDiskEntrySummary> = {
			label: directory.Name,
			id: directory.Path,
			path: directory.Path,
			children,
			nodeData: directory,
			icon: 'folder-close'
		};

		directory.Directories.map(this.convertDirectoryToNode).forEach(v => children.push(v));
		directory.Files.map(this.convertFileToNode).forEach(v => children.push(v));

		return node;
	}

	private convertFileToNode = (file: IFileSummary): ITeklyTreeNode<IDiskEntrySummary> => {
		const node: ITeklyTreeNode<IDiskEntrySummary> = {
			label: file.Name,
			id: file.Path,
			path: file.Path,
			nodeData: file,
			icon: 'document'
		};
		return node;
	}

	private onNodeDrag = (node: ITeklyTreeNode<IDiskEntrySummary>, evt: React.DragEvent<HTMLDivElement>) => {
		if (node.nodeData && node.nodeData.Type === 'Directory') {
			evt.preventDefault();
		}
	}

	private onNodeDrop = (node: ITeklyTreeNode<IDiskEntrySummary>, evt: React.DragEvent<HTMLDivElement>) => {
		if (node.nodeData && node.nodeData.Type === 'Directory') {
			evt.preventDefault();
			const item = evt.dataTransfer.items
			FileUploaderView.show(node.path, evt.dataTransfer.items);
			DiskEntriesView.uploadFile(node.path, evt.dataTransfer.files[0]);
		}
	}

	private static uploadFile(path: string, file: File) {
		// fetch(`/api/disk${path}/${file.name}`, { method: 'PUT', body: file });
	}
}
