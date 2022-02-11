import * as React from 'react';

import { Intent, Tag } from '@blueprintjs/core';
import { ITeklyTreeNode } from '~app/components/trees/TeklyTree';
import { IDictionary } from '~app/utility/Types';
import { IRouteDescriptor } from '../ApiTypes';

interface IRouteNode {
	route?: IRouteDescriptor;
	nodes: IRouteNode[];
	parent?: IRouteNode;
	path: string;
}

export function convertToTreeNodes(routes: IRouteDescriptor[]): Array<ITeklyTreeNode<IRouteDescriptor>> {
	const nodes = convertToRouteNodes(routes);
	const treeNodes = nodes.map(convertToTreeNode);

	return treeNodes;
}

function convertToRouteNodes(routes: IRouteDescriptor[]) {
	const visibleRoutes = routes.filter(x => !x.Hidden);
	const map: IDictionary<IRouteNode> = {};

	visibleRoutes.forEach(route => {
		const parent = getParent(route.Path, map);
		if (parent) {
			parent.nodes.push({
				path: route.Path,
				route,
				nodes: []
			});
		}
	});

	// Get the root nodes
	return Object.keys(map).map(k => map[k]).filter(x => !x.parent);
}

function convertToTreeNode(node: IRouteNode): ITeklyTreeNode<IRouteDescriptor> {
	const isDirectory = node.nodes.length !== 0;
	const children = isDirectory ? node.nodes.map(convertToTreeNode) : undefined;

	const verb = node.route ? node.route.Verb : '';

	return {
		id: node.path + verb,
		label: node.path,
		path: node.path,
		children,
		nodeData: node.route,
		secondaryLabel: verbTag(node.route)
	};
}

export function verbTag(routeDescriptor?: IRouteDescriptor) {
	if (!routeDescriptor) {
		return undefined;
	}

	const intent = routeDescriptor.Verb === 'DELETE' ? Intent.DANGER : Intent.SUCCESS;
	return <Tag intent={intent}>{routeDescriptor.Verb}</Tag>;
}

function parentPath(str: string): string | undefined {
	const index = str.lastIndexOf('/');
	if (index < 0) {
		return undefined;
	}
	return str.slice(0, index);
}

function getParent(path: string, map: IDictionary<IRouteNode>): IRouteNode | undefined {
	const originalParent = parentPath(path);

	if (!originalParent) {
		return undefined;
	}

	const parentsPath: string | undefined = originalParent;

	let parent = map[parentsPath];

	if (!parent) {
		parent = {
			path: parentsPath,
			nodes: [],
			parent: getParent(parentsPath, map)
		};

		if (parent.parent) {
			parent.parent.nodes.push(parent);
		}
		map[parentsPath] = parent;
	}

	return parent;
}
