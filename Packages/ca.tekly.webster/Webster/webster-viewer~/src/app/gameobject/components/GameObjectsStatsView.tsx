import * as React from 'react';

import Table, { ICellData, ITableHeading } from '~app/components/tables/Table';
import { IComponent, IGameObject, IHierarchy } from '../GameObjectTypes';

interface IProps {
	hierarchy?: IHierarchy;
}

interface IState {
	gameObjectStats: IGameObjectStats;
}

interface IGameObjectStats {
	totalGameObjects: number;
	totalComponents: number;
	stats: IStat[];
}

interface IStat {
	name: string;
	count: number;
	percentage: number;
}

const defaultStats: IGameObjectStats = {
	totalGameObjects: 0,
	totalComponents: 0,
	stats: []
};

export default class GameObjectsStatsView extends React.Component<IProps, IState> {

	constructor(props: IProps) {
		super(props);

		if (props.hierarchy) {
			this.state = {
				gameObjectStats: GameObjectsStatsView.generateStats(props.hierarchy)
			};
		} else {
			this.state = {
				gameObjectStats: defaultStats
			};
		}
	}

	public render() {
		return (
			<div className='gameobjects-stats-view'>
				<div className='heading'>
					<div>
						{`Total GameObjects: ${this.state.gameObjectStats.totalGameObjects}`}
					</div>
					<div className='float-right'>
						{`Total Components: ${this.state.gameObjectStats.totalComponents}`}
					</div>
				</div>
				<div className='table'>
					{this.renderTable()}
				</div>
			</div>
		);
	}

	private renderTable() {
		const headings: ITableHeading[] = [
			{ text: 'Component', sortable: true },
			{ text: 'Count', sortable: true  },
			{ text: 'Percentage', sortable: true },
		];

		const rows: ICellData[][] = this.state.gameObjectStats.stats.map(x => [
			{ text: x.name, sortValue: x.name },
			{ text: x.count.toString(), sortValue: x.count },
			{ text: `${(x.percentage * 100).toFixed(2)}%`, sortValue: x.percentage },
		]);

		return <Table headings={headings} rows={rows} stickyHeading={true} />;
	}

	public componentDidUpdate(prevProps: IProps) {
		if (prevProps === this.props) {
			return;
		}

		let stats = defaultStats;

		if (this.props.hierarchy) {
			stats = GameObjectsStatsView.generateStats(this.props.hierarchy);
		}

		this.setState({gameObjectStats: stats});
	}

	private static generateStats(hierarchy: IHierarchy): IGameObjectStats {
		const stats: IGameObjectStats = {
			totalGameObjects: 0,
			totalComponents: 0,
			stats: []
		};

		for (let index = 0; index < hierarchy.GameObjects.length; index++) {
			const gameObject = hierarchy.GameObjects[index];
			GameObjectsStatsView.processGameObject(gameObject, stats);
		}

		for (let index = 0; index < stats.stats.length; index++) {
			const stat = stats.stats[index];
			stat.percentage = stat.count / stats.totalComponents;
		}

		return stats;
	}

	private static processGameObject(gameobject: IGameObject, stats: IGameObjectStats) {
		if (!gameobject.IsScene) {
			stats.totalGameObjects += 1;
		}

		for (let index = 0; index < gameobject.Components.length; index++) {
			const component = gameobject.Components[index];
			GameObjectsStatsView.incrementComponent(component, stats);
		}

		if (gameobject.Children) {
			for (let index = 0; index < gameobject.Children.length; index++) {
				const gameObject = gameobject.Children[index];
				GameObjectsStatsView.processGameObject(gameObject, stats);
			}
		}
	}

	private static incrementComponent(component: IComponent, stats: IGameObjectStats) {
		let stat = stats.stats.find(x => x.name === component.Type);
		if (!stat) {
			stat = {
				count: 0,
				name: component.Type,
				percentage: 0
			};

			stats.stats.push(stat);
		}

		stats.totalComponents += 1;
		stat.count += 1;
	}
}
