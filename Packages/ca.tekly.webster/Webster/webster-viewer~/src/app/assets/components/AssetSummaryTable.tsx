import * as React from 'react';

import { IAssetSummary } from '../Types';

import SearchInput from '~app/components/SearchInput';
import Table, { ICellData, ITableHeading } from '~app/components/tables/Table';

export interface ITableData {
	headings: ITableHeading[];
	rows: ICellData[][];
}

interface IProps<T extends IAssetSummary> {
	assets: T[];
	getTableData: (assets: T[]) => ITableData;
}

interface IState {
	filter: string;
}

export class AssetSummaryTable<T extends IAssetSummary> extends React.Component<IProps<T>, IState> {
	public state: IState = {
		filter: ''
	};

	public render() {
		const assets = this.getFiltered(this.props.assets);
		const { headings, rows } = this.props.getTableData(assets);

		return (
			<div className='asset-table-container'>
				<SearchInput onChange={this.onFilterChanged} value={this.state.filter} placeholder='Filter'/>
				<div className='asset-table'>
					<Table headings={headings} rows={rows} stickyHeading/>
				</div>
			</div>
		);
	}

	private onFilterChanged = (filter: string) => this.setState({filter});

	private getFiltered(assets: T[]): T[] {
		try {
			const regex = new RegExp(this.state.filter, 'i');
			return assets.filter(x => regex.test(x.Name));
		} catch (err) {
			return assets.filter(x => x.Name.includes(this.state.filter));
		}
	}
}
