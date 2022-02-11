import * as React from 'react';

import { Card, Elevation, H5 } from '@blueprintjs/core';

import { IInfoSummary, IInfoItem } from '~app/utility/Types';
import { groupBy, mapMap } from '~app/utility/Utils';
import Table, { ITableHeading, ICellData } from '~app/components/tables/Table';
import { IAppState } from '~app/AppState';
import { refreshInfoSummary } from '../reducers/HomeActions';
import { connect } from 'react-redux';


interface IProps {
	infoSummary: IInfoSummary;
	refreshInfoSummary: () => void;
}

class HomeView extends React.Component<IProps> {

	public render() {
		return (
			<div className='home-view'>
				{this.props.infoSummary ? this.renderInfoSummary(this.props.infoSummary) : null}
			</div>
		);
	}

	private renderInfoSummary(infoSummary: IInfoSummary) {
		const categories = groupBy(infoSummary.Info, x => x.Category);
		const cards = mapMap(categories, HomeView.renderCategory);

		return (
			<div>
				<div className='home-view-table'>
					{this.renderInfoTable(infoSummary)}	
				</div>
			</div>
			
		);
	}

	private renderInfoTable(infoSummary: IInfoSummary) {
		const headers: ITableHeading[] = [ 
			{text: 'Category', sortable: true},
			{text: 'Name'},
			{text: 'Value'},
		];

		const cells = infoSummary.Info.map(HomeView.createRow);

		return <Table headings={headers} rows={cells} />;
	}

	private static createRow(info: IInfoItem): ICellData[] {
		return [
			{text: info.Category, sortValue: info.Category},
			{text: info.Name, sortValue: info.Name},
			{text: info.Value, sortValue: info.Value},
		]
	}

	private static renderCategory(category: string, info: IInfoItem[]) {
		const rows = info.map((x, index) => HomeView.row(x.Name, x.Value, index));

		return (
			<Card elevation={Elevation.THREE} className='home-view-card' key={category}>
				<H5>{category}</H5>
				<table className='home-data-table'>
					<tbody>
						{rows}
					</tbody>
				</table>
			</Card>
		);
	}

	private static row(title: string, data: string, index: number) {
		return (
			<tr key={index}>
				<td>{title}</td>
				<td>{data}</td>
			</tr>
		);
	}

	public componentDidMount() {
		this.props.refreshInfoSummary();
	}
}


const mapStateToProps = (state: IAppState) => {
	return {
		infoSummary: state.home.info
	};
};

export default connect(mapStateToProps, { refreshInfoSummary })(HomeView);
