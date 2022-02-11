import * as React from 'react';

import { connect } from 'react-redux';

import { IAppState } from '~app/AppState';
import SearchTree, { IFilterContext } from '~app/components/trees/SearchTree';
import { ITeklyTreeNode } from '~app/components/trees/TeklyTreeTypes';
import { IGameObject, IHierarchy } from '../GameObjectTypes';
import { refreshHierarchy } from '../reducers/GameObjectActions';
import GameObjectsStatsView from './GameObjectsStatsView';
import { convertHierarchyToTree } from './GameObjectsUtil';
import GameObjectView from './GameObjectView';

interface IGameObjectsViewState {
	nodes: Array<ITeklyTreeNode<IGameObject>>;
	selected?: IGameObject;
	selectedInstanceId?: number;
	expandedNodes: string[];
	selectedNode: string;
}

interface IProps {
	hierarchy: IHierarchy;
	refreshHierarchy: () => void;
}

class GameObjectsView extends React.Component<IProps, IGameObjectsViewState> {

	public state: IGameObjectsViewState = {
		nodes: [],
		selectedNode: '',
		expandedNodes: []
	};

	public componentDidUpdate(prevProps: IProps) {
		if (prevProps.hierarchy !== this.props.hierarchy) {
			const { hierarchy } = this.props;

			let selected: IGameObject | undefined = undefined;
			if (this.state.selectedInstanceId) {
				selected = this.findGameObjectByInstanceIdInGameObjects(hierarchy.GameObjects, this.state.selectedInstanceId)
			}
			this.setState({
				selected,
				nodes: convertHierarchyToTree(hierarchy)
			});
		}
	}

	public async componentDidMount() {
		this.props.refreshHierarchy();
	}

	public render() {
		return (
			<div className='gameobjects-view'>
				<div className='round-container'>
					<SearchTree nodes={this.state.nodes}
						placeholder='Search GameObjects'
						expandedNodes={this.state.expandedNodes}
						selectedNode={this.state.selectedNode}
						onNodesExpanded={this.onNodesExpanded}
						onNodeSelected={this.onNodeSelected}
						searchFilter={this.filterer}
					/>
				</div>
				<GameObjectView selected={this.state.selected} />
				<GameObjectsStatsView hierarchy={this.props.hierarchy} />
			</div>
		);
	}

	private onNodesExpanded = (expandedNodes: string[]) => {
		this.setState({expandedNodes});
	}

	private onNodeSelected = (node: ITeklyTreeNode<IGameObject>) => {
		this.setState({
			selectedNode: node.id,
			selected: node.nodeData,
			selectedInstanceId: node.nodeData ? node.nodeData.InstanceId : undefined
		});
	}

	private filterer = (filterContext: IFilterContext, node: ITeklyTreeNode<IGameObject>): boolean => {
		if (node.nodeData) {
			return filterContext.regex.test(node.nodeData.Path);
		}

		return false;
	}

	private findGameObjectByInstanceId(gameObject: IGameObject, instanceId: number): IGameObject | undefined {
		if (gameObject.InstanceId === instanceId ) {
			return gameObject;
		}

		return this.findGameObjectByInstanceIdInGameObjects(gameObject.Children, instanceId);
	}

	private findGameObjectByInstanceIdInGameObjects(gameObjects: IGameObject[], instanceId: number): IGameObject | undefined {
		for (let index = 0; index < gameObjects.length; index++) {
			const go = gameObjects[index];
			const found = this.findGameObjectByInstanceId(go, instanceId);

			if ( found ) {
				return found;
			}
		}

		return undefined;
	}
}

const mapStateToProps = (state: IAppState) => {
	return {
		hierarchy: state.gameObject.hierarchy
	};
};

export default connect(mapStateToProps, { refreshHierarchy })(GameObjectsView);
