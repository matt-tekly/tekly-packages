import { ITreeNode } from '@blueprintjs/core';
import { IDictionary } from './Types';

export function handleBooleanChange(handler: (checked: boolean) => void) {
	return (event: React.FormEvent<HTMLElement>) => handler((event.target as HTMLInputElement).checked);
}

export function handleNumberChange(handler: (value: number) => void) {
	return handleStringChange(value => handler(+value));
}

export function handleStringChange(handler: (value: string) => void) {
	return (event: React.FormEvent<HTMLElement>) => handler((event.target as HTMLInputElement).value);
}

export function pathToTreeNodeField(path: number[], field: string): string[] {
	const resultPath: string[] = [];
	for (let i = 0; i < path.length; i++) {
		const n = path[i];
		if (i > 0) {
			resultPath.push('childNodes');
		}

		resultPath.push(`${n}`);
	}

	resultPath.push(field);

	return resultPath;
}

export function toNodePathsMap(nodes: ITreeNode[], path: number[], nodePaths: IDictionary<number[]>) {
	path.push(0);
	const last = path.length - 1;

	nodes.forEach((node, index) => {
		path[last] = index;
		const nodePath = path.slice(0);
		nodePaths[node.id] = nodePath;

		if (node.childNodes) {
			toNodePathsMap(node.childNodes, nodePath.slice(0), nodePaths);
		}
	});
}
