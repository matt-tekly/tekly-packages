import * as React from 'react';

import { IComponentRendererProps } from '../../GameObjectTypes';
import { getRenderer } from './ComponentsProvider';

export default class ComponentRenderer extends React.Component<IComponentRendererProps> {
	public render() {
		const { component } = this.props;
		const Render = getRenderer(component);
		return (
			<Render {...this.props} />
		);
	}
}
