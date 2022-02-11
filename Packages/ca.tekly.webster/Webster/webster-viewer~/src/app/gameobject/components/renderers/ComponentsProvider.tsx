import { IDictionary } from '~app/utility/Types';
import { IComponent, IComponentRendererProps } from '../../GameObjectTypes';

import { ComponentRendererImpl } from './ComponentRendererImpl';
import { TransformRenderer } from './TransformRenderer';

const rendererMap: IDictionary<new (props: IComponentRendererProps) => ComponentRendererImpl> = {
	Transform: TransformRenderer
};

export function getRenderer(component: IComponent) {

	const renderer = rendererMap[component.Type];

	if (renderer) {
		return renderer;
	}

	return ComponentRendererImpl;
}
