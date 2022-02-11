import * as React from 'react';

import { IAssetSummary, ISpriteSummary } from '../Types';

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

export class SpritesSummaryTable<T extends IAssetSummary> extends React.Component<IProps<T>, IState> {
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
					<Table headings={headings} rows={rows} stickyHeading cellRenderer={this.renderCell} />
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
    
    private renderCell = (cell: ICellData, idx: number) =>  {
        if (idx === 0) {
            let spriteData = cell.userData as ISpriteSummary;
            return <a href={`api/sprites/id/${spriteData.InstanceId}`} target='_blank'>{cell.text}</a>;
        }

        if (idx === 5) {
            let spriteData = cell.userData as ISpriteSummary;
            return <a href={`api/textures/id/${spriteData.TextureInstanceId}`} target='_blank'>{cell.text}</a>;
        }

        return cell.text
	}
}
