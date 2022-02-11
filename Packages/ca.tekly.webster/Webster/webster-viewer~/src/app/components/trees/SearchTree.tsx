import * as React from 'react';

import { Callout, Intent } from '@blueprintjs/core';

import SearchInput from '~app/components/SearchInput';
import { ITeklyTreeProps, ITeklyTreeNode } from './TeklyTreeTypes';
import { TeklyTree } from './TeklyTree';

export interface IFilterContext {
	filter: string;
	regex: RegExp;
}

interface ISearchTreeProps<T> extends ITeklyTreeProps<T> {
	placeholder: string;
	searchFilter?: (filterContext: IFilterContext, node: ITeklyTreeNode<T>) => boolean;
}

interface ISearchTreeState extends IFilterContext {
	filterError: string;
}

export default class SearchTree<T> extends React.Component<ISearchTreeProps<T>, ISearchTreeState> {

	constructor(props: ISearchTreeProps<T>) {
		super(props);

		this.state = {
			filter: '',
			regex: new RegExp(''),
			filterError: ''
		};
	}

	public render() {
		return (
			<div className='tekly-search-tree'>
				<SearchInput small placeholder={this.props.placeholder} onChange={this.handleFilterChange} value={this.state.filter} />
				{this.renderFilterError()}
				{this.renderTree()}
			</div>
		);
	}

	private renderTree() {
		const actualFilterer = this.createFilterer(this.props.searchFilter || this.defaultFilterer);
		const filterer = this.state.filter ? actualFilterer : undefined;

		return <TeklyTree {...this.props} filterer={filterer} flat={!!this.state.filter}/>;
	}

	private renderFilterError() {
		if (this.state.filterError) {
			return <Callout style={{margin: '2px'}} intent={Intent.DANGER} icon='error'>{this.state.filterError.toString()}</Callout>;
		}

		return <span />;
	}

	private handleFilterChange = (filter: string) => {
		let regex = this.state.regex;
		let filterError = '';

		try {
			regex = new RegExp(filter, 'i');
		} catch (err) {
			filterError = err;
		}

		this.setState({
			filter,
			regex,
			filterError
		});
	}

	private createFilterer = (func: (filterContext: IFilterContext, node: ITeklyTreeNode<T>) => boolean) => {
		return (n: ITeklyTreeNode<T>): boolean => {
			return func(this.state, n);
		};
	}

	private defaultFilterer = (filterContext: IFilterContext, node: ITeklyTreeNode<T>) => {
		if (node.children && node.children.length) {
			return false;
		}

		return filterContext.regex.test(node.id);
	}
}
