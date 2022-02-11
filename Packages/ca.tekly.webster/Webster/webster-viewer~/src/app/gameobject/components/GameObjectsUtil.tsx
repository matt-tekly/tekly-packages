import * as React from 'react';

import { ITeklyTreeNode } from '~app/components/trees/TeklyTreeTypes';
import { IGameObject, IHierarchy } from '../GameObjectTypes';

export function convertHierarchyToTree(hierarchy: IHierarchy): Array<ITeklyTreeNode<IGameObject>> {
	return hierarchy.GameObjects.map(convertToTreeNode);
}

function convertToTreeNode(gameObject: IGameObject): ITeklyTreeNode<IGameObject> {
	const isParent = gameObject.Children.length > 0;
	const children = isParent ? gameObject.Children.map(convertToTreeNode) : undefined;

	const style = gameObject.Active ? 'label-enabled' : 'label-disabled';

	return {
		id: gameObject.Path + gameObject.InstanceId,
		path: gameObject.Path,
		label: (<span className={style}>{gameObject.Name}</span>),
		children,
		nodeData: gameObject,
	};
}
