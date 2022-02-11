import * as React from 'react';

import { connect } from 'react-redux';

import { Checkbox, Intent, Tag } from '@blueprintjs/core';

import { IComponent, IGameObject } from '../GameObjectTypes';
import ComponentRenderer from './renderers/ComponentRenderer';
import { handleBooleanChange } from '~app/utility/BlueprintUtils';
import { IAppState } from '~app/AppState';
import { setComponentEnabled, setGameObjectActive } from '../reducers/GameObjectActions';

interface IGameObjectViewProps {
	selected?: IGameObject;
	setGameObjectActive: (instanceId: number, active: boolean) => void;
	setComponentEnabled: (instanceId: number, enabled: boolean) => void;
}

class GameObjectView extends React.Component<IGameObjectViewProps> {
	

	public render() {
		return (
			<div className='gameobject-view'>
				{this.renderInternal()}
			</div>
		);
	}

	private renderInternal() {
		if (!this.props.selected) {
			return (
				<div className='no-gameobject-selected'>
					<span>No GameObject Selected</span>
				</div>
			);
		}

		const gameObject = this.props.selected;

		if (this.props.selected.IsScene) {
			return this.renderScene(gameObject);
		}

		return this.renderGameObject(gameObject);
	}

	private renderScene(gameObject: IGameObject) {
		return (
			<div>
				{`SCENE: ${gameObject.Name}`}
			</div>
		);
	}

	private renderGameObject(gameObject: IGameObject) {
		const components = gameObject.Components.map(this.renderComponent);
		return (
			<div>
				{this.renderHeader(gameObject)}
				{components}
			</div>
		);
	}

	private renderHeader(gameObject: IGameObject) {
		return (
			<div className='heading'>
				<Checkbox checked={gameObject.ActiveSelf} label={gameObject.Name} large onChange={this.onToggleGameObjectActive} />
				<div className='layer'>
					<Tag intent={Intent.PRIMARY}>{gameObject.Layer}</Tag>
				</div>
				<small>{gameObject.Path}</small>
			</div>
		);
	}

	private onToggleGameObjectActive = handleBooleanChange(_ => {
		const gameObject = this.props.selected;
		if (!gameObject) {
			return 
		}

		this.props.setGameObjectActive(gameObject.InstanceId, !gameObject.ActiveSelf);
	});

	private renderComponent = (component: IComponent, key: number) => {
		return <ComponentRenderer component={component} key={key} setComponentEnabled={this.props.setComponentEnabled} />;
	}
}

const mapStateToProps = (state: IAppState) => {
	return {}
};


export default connect(mapStateToProps, { setComponentEnabled, setGameObjectActive })(GameObjectView);
