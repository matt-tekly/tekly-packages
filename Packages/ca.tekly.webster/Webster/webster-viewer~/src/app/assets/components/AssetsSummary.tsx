import * as React from 'react';

import { AssetSummaryTypes, IAssetsSummary } from '../Types';

import Table, { ICellData, ITableHeading } from '~app/components/tables/Table';

interface IProps {
	assets: IAssetsSummary;
}

export class AssetsSummary extends React.Component<IProps> {
	public render() {
		const headings: ITableHeading[] = [
			{ text: 'Asset Type', sortable: true },
			{ text: 'Count', sortable: true  },
		];

		const rows: ICellData[][] = AssetSummaryTypes.map(key => {
			const x = this.props.assets[key];
			return [
				{ text: key, sortValue: key },
				{ text: x.length.toString(), sortValue: x.length },
			];
		});

		return (
			<div className='asset-table-container'>
				<Table headings={headings} rows={rows}/>
			</div>
		);
	}
}
