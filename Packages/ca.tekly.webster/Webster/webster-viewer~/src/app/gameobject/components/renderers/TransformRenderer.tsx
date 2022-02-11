import * as React from 'react';

import { ITransform, IVector3 } from '../../GameObjectTypes';
import { ComponentRendererImpl } from './ComponentRendererImpl';

export class TransformRenderer extends ComponentRendererImpl {
	protected renderBody() {
		const { component } = this.props;
		const transform = component.Values as ITransform;
		return (
			<div>
				{this.renderVector('Position', transform.Position)}
				{this.renderVector('Rotation', transform.Rotation)}
				{this.renderVector('Scale', transform.Scale)}
			</div>
		);
	}
	private renderVector(name: string, vector: IVector3) {
		return (
			<div className='vector3'>
				<span>{name}</span>
				<span>{`x: ${vector.x.toFixed(3)}`}</span>
				<span>{`y: ${vector.y.toFixed(3)}`}</span>
				<span>{`z: ${vector.z.toFixed(3)}`}</span>
			</div>
		);
	}
}
