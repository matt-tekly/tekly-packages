import * as React from 'react';

import { del, push } from 'object-path-immutable';
import { TeklyTreeNode } from './TeklyTreeNode';

import { ITeklyTreeNode, ITeklyTreeProps } from './TeklyTreeTypes';

import './TeklyTree.scss';

interface INodeRender<T> {
	depth: number;
	node: ITeklyTreeNode<T>;
}

function onlyUnique<T>(value: T, index: number, self: T[]) {
	return self.indexOf(value) === index;
}

export class TeklyTree<T> extends React.Component<ITeklyTreeProps<T>> {

	constructor(props: ITeklyTreeProps<T>) {
		super(props);
	}

	public render() {
		const nodesToRender: Array<INodeRender<T>> = [];
		this.flatten(this.props.nodes, nodesToRender, 0);

		const nodeElements = nodesToRender.map(this.renderNode);
		return (
			<div className='tekly-tree'>
				{nodeElements}
			</div>
		);
	}

	private renderNode = (nodeRender: INodeRender<T>) => {
		const { depth, node } = nodeRender;
		const isExpanded = this.isExpanded(node);
		const isSelected = node.id === this.props.selectedNode;

		return <TeklyTreeNode
			node={node}
			depth={depth}
			expanded={isExpanded}
			selected={isSelected}
			setExpanded={this.setExpanded}
			setSelected={this.setSelected}
			onNodeDrag={this.props.onNodeDrag}
			onNodeDrop={this.props.onNodeDrop}
			flat={this.props.flat}
			key={node.id}
		/>;
	}

	private setExpanded = (node: ITeklyTreeNode<T>, expanded: boolean, recursive: boolean) => {
		if (expanded) {
			if (recursive) {
				const expandedNodes = this.getChildren(node)
					.map(n => n.id)
					.concat(this.props.expandedNodes)
					.filter(onlyUnique);

				this.setState({expandedNodes});
			} else {
                this.props.onNodesExpanded(push(this.props.expandedNodes, undefined, node.id));
			}
		} else {
			if (recursive) {
				const nodesToCollapse = this.getChildren(node).map(n => n.id);
				const expandedNodes = this.props.expandedNodes.filter(x => nodesToCollapse.indexOf(x) === -1);
				this.props.onNodesExpanded(expandedNodes);
			} else {
				const index = this.props.expandedNodes.findIndex(x => x === node.id);
				this.props.onNodesExpanded(del(this.props.expandedNodes, `${index}`));
			}
		}
	}

	private getChildren = (node: ITeklyTreeNode<T>) => {
		const children: Array<ITeklyTreeNode<T>> = [];
		this.getChildrenImpl(node, children);

		return children;
	}

	private getChildrenImpl = (node: ITeklyTreeNode<T>, children: Array<ITeklyTreeNode<T>>) => {
		children.push(node);
		if (node.children && node.children.length > 0) {
			for (let index = 0; index < node.children.length; index++) {
				const child = node.children[index];
				this.getChildrenImpl(child, children);
			}
		}
	}

	private flatten = (nodes: Array<ITeklyTreeNode<T>>, dest: Array<INodeRender<T>>, depth: number) => {
		for (let index = 0; index < nodes.length; index++) {
			const node = nodes[index];

			if (!this.shouldFilter(node)) {
				dest.push({
					depth,
					node
				});
			}

			if (node.children && node.children.length && (this.props.flat || this.isExpanded(node))) {
				const nextDepth = this.props.flat ? 0 : depth + 1;
				this.flatten(node.children, dest, nextDepth);
			}
		}
	}

	private shouldFilter(node: ITeklyTreeNode<T>): boolean {
		if (this.props.filterer) {
			return !this.props.filterer(node);
		}

		return false;
	}

	private isExpanded = (node: ITeklyTreeNode<T>): boolean => {
		return this.props.expandedNodes.findIndex(x => x === node.id) !== -1;
	}

	private setSelected = (node: ITeklyTreeNode<T>): void => {
		if (this.props.selectedNode === node.id) {
			return;
		}

		this.props.onNodeSelected(node);
	}
}
