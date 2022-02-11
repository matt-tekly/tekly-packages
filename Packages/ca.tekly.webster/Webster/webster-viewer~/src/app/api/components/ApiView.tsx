import * as React from 'react';
import { connect } from 'react-redux';

import SelectableTextArea from '~app/components/SelectableTextArea';
import SearchTree from '~app/components/trees/SearchTree';
import { ITeklyTreeNode } from '~app/components/trees/TeklyTreeTypes';
import { IRouteDescriptor, IRouteInfo, IRouteResponse, IRouteState } from '../ApiTypes';
import { convertToTreeNodes } from './ApiTreeViewUtil';
import RouteView from './RouteView';
import { canBeTable, toTable } from './ApiUtils';
import Table from '~app/components/tables/Table';
import { IAppState } from '~app/AppState';
import { executeRoute, refreshRoutes } from '../reducers/ApiReducer';
import { IDictionary } from '~app/utility/Types';
import { setRouteProperty } from '../reducers/ApiActions';


interface IApiViewState {
	filter: string;
	nodes: Array<ITeklyTreeNode<IRouteDescriptor>>;
	expandedNodes: string[];
	selectedNode: string;
}

interface IProps {
	routeInfo: IRouteInfo;
	routeStates: IDictionary<IRouteState>
	routeResponses: IDictionary<IRouteResponse>;

	refreshRoutes: () => void;
	setRouteProperty: (route: string, property: string, value: any) => void;
	executeRoute: (route: IRouteDescriptor, properties: IDictionary<any>) => void;
}

class ApiView extends React.Component<IProps, IApiViewState> {

	public state: IApiViewState = {
		filter: '',
		nodes: [],
		expandedNodes: [],
		selectedNode: ''
	};

	public componentDidMount() {
		this.props.refreshRoutes();
	}

	componentDidUpdate(prevProps: IProps, prevState: IApiViewState) {
        if (prevProps.routeInfo !== this.props.routeInfo) {
			const nodes = convertToTreeNodes(this.props.routeInfo.Routes);
			this.setState({ nodes });
        }
    }

	public render() {
		return (
			<div className='api-root'>
				<div className='api-container'>
					<div className='round-container'>
						<SearchTree nodes={this.state.nodes} onNodeSelected={this.onNodeSelected} onNodesExpanded={this.onNodesExpanded} placeholder='Search' expandedNodes={this.state.expandedNodes} selectedNode={this.state.selectedNode} />
					</div>
					<div className='api-info-container'>
						{this.renderSelectedRoute()}
						{this.renderResponseView()}
					</div>
				</div>
			</div>
		);
	}

	private onNodesExpanded = (expandedNodes: string[]) => {
		this.setState({expandedNodes});
	}

	private onNodeSelected = (node: ITeklyTreeNode<IRouteDescriptor>): void => {
		if (this.state.selectedNode === node.id) {
			return;
		}

		this.setState({
			selectedNode: node.id
		});
	}

	private renderSelectedRoute = () => {
		if (this.state.selectedNode) {
			const route = this.props.routeInfo.Routes.find(r => (r.Path + r.Verb) === this.state.selectedNode);
			
			if (route) {
				const key = (route.Path + route.Verb);
				
				const routeState = this.props.routeStates[key] || {};
				const properties = routeState.values || {};
				const set = (property:string , value: any) => this.props.setRouteProperty(key, property, value);

				return <RouteView route={route} properties={properties} setRouteProperty={set} executeRoute={this.props.executeRoute} />;
			}
		}

		return (
			<div className='api-route-no-selection'>
				No Route Selected
			</div>
		);
	}

	private renderResponseView() {
		const response = this.props.routeResponses[this.state.selectedNode
		]
		if (!response) {
			return <div className={`api-response`}/>;
		}

		const { result } = response

		if (!result) {
			return this.renderResponseString(response.statusText, false);
		}

		if (canBeTable(result)) {
			const tableData = toTable(result);
			return (
				<div>
					<Table headings={tableData.headings} rows={tableData.values} />
				</div>
			)
		}
		
		if (typeof result === 'string') {
			return this.renderResponseString(result, false);
		} 

		return this.renderResponseString(JSON.stringify(result, null, 4), true);
	}

	private renderResponseString(str: string | undefined, isObject: boolean) {
		const preStyle = isObject ? 'api-response-object' : 'api-response-string';

		return (
			<div className={`api-response ${preStyle}`}>
				<SelectableTextArea value={str || ''} />
			</div>
		);
	}
}

const mapStateToProps = (state: IAppState) => {
	return {
		routeInfo: state.api.routeInfo,
		routeStates: state.api.routeStates,
		routeResponses: state.api.routeResponses
	};
};

export default connect(mapStateToProps, {refreshRoutes, setRouteProperty, executeRoute})(ApiView);