import * as React from 'react';

import Table, { ICellData, ITableHeading } from '~app/components/tables/Table';
import { IFramelineData } from '../../FramelineTypes';

interface IProps {
	data: IFramelineData;
}

const headings: ITableHeading[] = [
	{ text: 'Type', sortable: true },
	{ text: 'Id', sortable: true },
	{ text: 'Start Time', sortable: true },
	{ text: 'End Time', sortable: true },
	{ text: 'Start Frame', sortable: true },
	{ text: 'End Frame', sortable: true },
	{ text: 'Duration', sortable: true },
];

export class FramelineTable extends React.Component<IProps, {}> {

	public render() {
		const cellData: ICellData[][] = this.props.data.Events.map(x => {
			return [
				{ text: x.EventType, sortValue: x.EventType },
				{ text: x.Id, sortValue: x.Id },
				{ text: (x.StartTime / 1000).toFixed(4), sortValue: x.StartTime },
				{ text: (x.EndTime / 1000).toFixed(4), sortValue: x.EndTime },
				{ text: x.StartFrame.toFixed(), sortValue: x.StartFrame },
				{ text: x.EndFrame.toFixed(), sortValue: x.EndFrame },
				{ text: ((x.EndTime - x.StartTime) / 1000).toFixed(4), sortValue: x.EndTime - x.StartTime },
			];
		});

		return (
			<div className='frameline-table-container'>
				<div className='frameline-table'>
					<Table headings={headings} rows={cellData} stickyHeading/>
				</div>
			</div>
		);
	}
}
