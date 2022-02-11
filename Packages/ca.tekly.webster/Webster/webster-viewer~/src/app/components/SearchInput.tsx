import * as React from 'react';

import { Button, InputGroup } from '@blueprintjs/core';
import { handleStringChange } from '~app/utility/BlueprintUtils';

interface ISearchInputProps {
	placeholder: string;
	small?: boolean;
	value: string;

	onChange: (value: string) => void;
}

export default class SearchInput extends React.Component<ISearchInputProps> {

	public render() {
		const clearButton = (
			<Button
				icon='delete'
				minimal={true}
				onClick={this.handleClearFilter}
			/>
		);

		return (
			<div style={{margin: '2px'}}>
				<InputGroup
					className='bp3-round'
					small={this.props.small}
					placeholder={this.props.placeholder}
					leftIcon='search'
					rightElement={clearButton}
					onChange={this.handleFilterChange}
					value={this.props.value}
				/>
			</div>
		);
	}

	private handleFilterChange = handleStringChange(value => this.props.onChange(value));
	private handleClearFilter = () => this.props.onChange('');

}
