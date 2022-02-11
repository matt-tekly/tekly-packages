import * as React from 'react';

import { Checkbox } from '@blueprintjs/core';
import { IComponentRendererProps } from '~app/gameobject/GameObjectTypes';
import { handleBooleanChange } from '~app/utility/BlueprintUtils';

export class ComponentRendererImpl extends React.Component<IComponentRendererProps> {
	public render() {
		return (
			<div className='component'>
				{this.renderHeader()}
				{this.renderBodyContainer()}
			</div>
		);
	}
	protected renderHeader() {
		const { component } = this.props;
		return (
			<div className='component-heading'>
				<Checkbox checked={component.EnabledSelf} label={component.Type} onChange={this.onToggleComponentEnabled} disabled={!component.CanBeDisabled} />
			</div>
		);
	}

	protected renderBodyContainer() {
		return (
			<div className='component-body'>
				{this.renderBody()}
			</div>
		);
	}

	protected renderBody() {
		const { component } = this.props;
		return (
			<div className='component-values-json'>
				{JSON.stringify(component.Values, null, 4)}
			</div>
		);
	}

	private onToggleComponentEnabled = handleBooleanChange(_ => {
		const component = this.props.component;
		this.props.setComponentEnabled(component.InstanceId, !component.EnabledSelf);
	});
}

