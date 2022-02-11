import { MaybeElement, IconName } from "@blueprintjs/core";

export interface ITeklyTreeNode<T = {}> {
	id: string;
	label: string | JSX.Element;
	path: string;
	secondaryLabel?: string | MaybeElement;
	icon?: IconName | MaybeElement;
	nodeData?: T;
	children?: Array<ITeklyTreeNode<T>>;
}

export interface ITeklyTreeProps<T = {}> {
    nodes: Array<ITeklyTreeNode<T>>;
    expandedNodes: string[];
    selectedNode: string;
    flat?: boolean;
	filterer?: (node: ITeklyTreeNode<T>) => boolean;
	onNodeSelected: (node: ITeklyTreeNode<T>) => void;
	onNodeDrag?: (node: ITeklyTreeNode<T>, evt: React.DragEvent<HTMLDivElement>) => void;
    onNodeDrop?: (node: ITeklyTreeNode<T>, evt: React.DragEvent<HTMLDivElement>) => void;
    onNodesExpanded: (expandedNodes: string[]) => void;
}

export interface ITeklyTreeState {
	expandedNodes: string[];
	selectedNode: string;
}