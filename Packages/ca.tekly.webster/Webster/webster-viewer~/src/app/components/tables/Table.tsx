import * as React from 'react';

import { Button, HTMLTable } from '@blueprintjs/core';

export interface ITableHeading {
	text: string;
	sortable?: boolean;
}

export interface ICellData {
	text: string;
	sortValue: string | number;
	userData?: any;
}

interface IProps {
	headings: ITableHeading[];
	rows: ICellData[][];
	stickyHeading?: boolean;
	cellRenderer?: (cell: ICellData, idx: number) => JSX.Element | string;
}

interface IState {
	sortColumn: number;
	sortColumnName: string;
	sortAsc: boolean;
	columnStyles: string[];
}

const MAX_NO_BREAK_LENGTH = 30;

export default class Table extends React.Component<IProps, IState> {
	constructor(props: IProps) {
		super(props);

		this.state = {
			sortColumn: 0,
			sortColumnName: '',
			sortAsc: true,
			columnStyles: ['']
		};
	}

	public static getDerivedStateFromProps(props: IProps, state: IState): Partial<IState> | null {
		const { sortColumn } = state;

		if (sortColumn >= props.headings.length) {
			return { 
				sortColumn: 0, 
				sortColumnName: props.headings[0].text,
				columnStyles: Table.calculateColumnStyles(props.rows, props.headings.length)
			};
		} else {
			const oldColumn = state.sortColumnName;
			const newColumn = props.headings[sortColumn].text;

			if (oldColumn !== newColumn) {
				return { 
					sortColumn: 0, 
					sortColumnName: props.headings[0].text,
					columnStyles: Table.calculateColumnStyles(props.rows, props.headings.length)
				};
			}
		}

		if (props.headings.length !== state.columnStyles.length ) {
			return { 
				columnStyles: Table.calculateColumnStyles(props.rows, props.headings.length)
			};
		}

		return null;
	}

	private static calculateColumnStyles(rows: ICellData[][], columnCount: number): string[] {
		let counts = new Array<number>(columnCount);
		counts.fill(0);
		
		for(let y = 0; y < rows.length; y++) {
			let row = rows[y];
			for(let x = 0; x < row.length; x++) {
				counts[x] = Math.max(counts[x], row[x].text.length);
			}
		}

		const styles = new Array<string>(columnCount);

		for(let i = 0; i < columnCount; i++) {
			styles[i] = counts[i] <= MAX_NO_BREAK_LENGTH ? 'no-wrap' : '';
		}

		return styles;
	}

	private sortRows = (a: ICellData[], b: ICellData[]) => {
		const { sortColumn } = this.state;
		const av = a[sortColumn].sortValue;
		const bv = b[sortColumn].sortValue;

		const sortMult = this.state.sortAsc ? 1 : -1;
		
		if (typeof av === 'string' && typeof bv === 'string')  {
			return av.localeCompare(bv, undefined, { sensitivity: 'accent' }) * sortMult;
		} else {
			if (av < bv) { return -1 * sortMult; }
			if (av > bv) { return 1 * sortMult; }
		}

		return 0;
	}

	public render() {
		const rows = this.props.rows.sort(this.sortRows);
		const styles = this.props.stickyHeading ? 'tekly-table sticky' : 'tekly-table';

		return (
			<HTMLTable condensed striped style={{width: '100%'}} className={styles}>
				<thead>
					<tr>
						{this.props.headings.map(this.renderHeadingCell)}
					</tr>
				</thead>
				<tbody>
					{rows.map(this.renderRow)}
				</tbody>
			</HTMLTable>
		);
	}

	private renderHeadingCell = (heading: ITableHeading, index: number) => {
		const style = { padding: 0, paddingBottom: 2 };

		const sortIcon = this.getSortIcon(index);
		const className = sortIcon ? 'sort-column' : 'column';

		if (heading.sortable) {
			return (
				<th style={style} key={index} className={className}>
					<Button minimal fill alignText='left' rightIcon={sortIcon} onClick={() => this.setSortBy(index)}>
						<span className='heading-text'>{heading.text}</span>
					</Button>
				</th>
			);
		}

		return (
			<th style={style} key={index} className={className}>
				<Button minimal fill alignText='left'>
					<span className='heading-text'>{heading.text}</span>
				</Button>
			</th>
		);
	}

	private setSortBy = (sortColumn: number) => {
		const sortDir = this.state.sortColumn === sortColumn ? !this.state.sortAsc : true;

		this.setState({
			sortAsc: sortDir,
			sortColumn,
			sortColumnName: this.props.headings[sortColumn].text
		});
	}

	private getSortIcon(index: number) {
		if (this.state.sortColumn !== index) {
			return undefined;
		}

		return this.state.sortAsc ? 'sort-asc' : 'sort-desc';
	}

	private renderRow = (row: ICellData[], index: number) => {
		const cellRenderer = this.props.cellRenderer || this.renderCell;
		return (
			<tr key={index} >
				{row.map((cell, idx) => <td key={idx} className={this.state.columnStyles[idx]}>{cellRenderer(cell, idx)}</td>)}
			</tr>
		);
	}

	private renderCell = (cell: ICellData, idx: number): JSX.Element | string =>  {
		return cell.text;
	}
}
