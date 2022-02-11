import * as React from 'react';

import { Icon } from '@blueprintjs/core';

import { ITeklyTreeNode } from './TeklyTreeTypes';

import './TeklyTree.scss';


interface ITeklyTreeNodeProps<T = {}> {
	node: ITeklyTreeNode<T>;
	depth: number;
	expanded: boolean;
	selected: boolean;
	flat?: boolean;

	setExpanded: (node: ITeklyTreeNode<T>, expanded: boolean, recursive: boolean) => void;
	setSelected: (node: ITeklyTreeNode<T>) => void;

	onNodeDrag?: (node: ITeklyTreeNode<T>, evt: React.DragEvent<HTMLDivElement>) => void;
	onNodeDrop?: (node: ITeklyTreeNode<T>, evt: React.DragEvent<HTMLDivElement>) => void;
}

const MARGIN_SIZE = 16;

export class TeklyTreeNode<T> extends React.PureComponent<ITeklyTreeNodeProps<T>> {

	public render() {
		const { depth, node } = this.props;

		const style = {
			paddingLeft: `${depth * MARGIN_SIZE}px`
		};

		const className = this.props.selected ? 'node-selected' : 'node';

		return (
			<div style={style} className={className} onClick={this.setSelected} onDrop={this.onDrop} onDragOver={this.onDrag} >
				{this.renderCaret()}
				<span>
					{this.renderIcon()}
					<span className='node-label'>{node.label}</span>
				</span>
				{this.renderSecondaryLabel()}
			</div>
		);
	}

	private renderCaret() {
		if (this.props.flat || !this.props.node.children || !this.props.node.children.length) {
			return <span />;
		}

		const icon = !this.props.expanded ? 'chevron-right' : 'chevron-down';
		return <Icon className='node-caret' icon={icon} onClick={this.toggleExpanded} />;
	}

	private renderIcon() {
		if (typeof (this.props.node.icon) === 'string') {
			return <Icon icon={this.props.node.icon} className='node-icon' />;
		}
		return this.props.node.icon || undefined;
	}

	private renderSecondaryLabel() {
		return this.props.node.secondaryLabel || <span />;
	}

	private toggleExpanded = (event: React.MouseEvent<HTMLElement, MouseEvent>) => {
		event.stopPropagation();
		this.props.setExpanded(this.props.node, !this.props.expanded, event.shiftKey);
	}

	private setSelected = () => {
		this.props.setSelected(this.props.node);
	}

	private onDrop = (evt: React.DragEvent<HTMLDivElement>) => {
		if (this.props.onNodeDrop) {
			this.props.onNodeDrop(this.props.node, evt);
		}
	}

	private onDrag = (evt: React.DragEvent<HTMLDivElement>) => {
		if (this.props.onNodeDrag) {
			this.props.onNodeDrag(this.props.node, evt);
		}
	}
}
