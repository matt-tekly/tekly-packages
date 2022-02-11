import * as React from 'react';

import { Button, ControlGroup, FormGroup, HTMLSelect, InputGroup, NumericInput } from '@blueprintjs/core';

import { IDictionary } from '~app/utility/Types';
import { IRouteDescriptor, IValueDescriptor } from '../ApiTypes';
import { verbTag } from './ApiTreeViewUtil';

interface IRouteViewProps {
	route: IRouteDescriptor;
	properties: IDictionary<any>;

	setRouteProperty:(property: string, value: any) => void;
	executeRoute: (route: IRouteDescriptor, properties: IDictionary<any>) => void;
}

export default class RouteView extends React.Component<IRouteViewProps> {

	constructor(props: IRouteViewProps) {
		super(props);
	}

	public componentDidUpdate(prevProps: IRouteViewProps) {
		if (prevProps.route !== this.props.route) {
			const values: IDictionary<any> = {};
			this.props.route.QueryParams.forEach(param => {
				values[param.Name] = param.DefaultValue;
			});

			this.setState({values});
		}
	}

	public render() {
		const { route } = this.props;

		return (
			<div className='api-route'>
				<div className='api-route-header'>
					<div>
						<p>{verbTag(route)} {route.Path}</p>
						{this.renderDescription()}
					</div>
					<div>
						<Button intent='primary' onClick={this.activateRoute} small>Submit</Button>
					</div>
				</div>
				<div className='api-route-content'>
					{route.QueryParams.filter(this.shouldShowParam).map(this.renderParam)}
				</div>
			</div>
		);
	}

	private renderDescription() {
		if (!this.props.route.Description) {
			return undefined;
		}

		return <div className='bp3-text-muted bp3-text-small'>{this.props.route.Description}</div>;
	}

	private shouldShowParam = (param: IValueDescriptor) => {
		return param.Type !== 'request' && param.Type !== 'response';
	}

	private renderParam = (param: IValueDescriptor, index: number) => {
		const handleReset = () => {
			this.props.setRouteProperty(param.Name, param.DefaultValue);
		};

		return (
			<div key={index}>
				<FormGroup
					label={param.Name}
					helperText=''
					labelFor='text-input'
				>
					<ControlGroup>
						<Button icon='reset' onClick={handleReset} small />
						{this.renderParamImpl(param)}
					</ControlGroup>
				</FormGroup>
			</div>
		);
	}

	private renderParamImpl(param: IValueDescriptor) {
		switch (param.Type) {
			case 'long':
			case 'double':
				return this.renderNumberParam(param);
			case 'string':
			case 'enum':
				return this.renderTextParam(param);
			case 'boolean':
				return this.renderBooleanParam(param);
		}

		return undefined;
	}

	private renderNumberParam = (param: IValueDescriptor) => {
		const handler = (_: number, valAsString: string) => {
			this.props.setRouteProperty(param.Name, valAsString);
		};

		const value = this.props.properties[param.Name];

		return (
			<NumericInput id={param.Name} minorStepSize={0.1} value={value} onValueChange={handler} />
		);
	}

	private renderTextParam = (param: IValueDescriptor) => {
		const selectHandler = (e: React.FormEvent<HTMLSelectElement>) => {
			this.props.setRouteProperty(param.Name, e.currentTarget.value);
		};

		const value = this.props.properties[param.Name];

		if (param.Values && param.Values.length > 0) {
			return (
				<HTMLSelect options={param.Values} value={value} onChange={selectHandler} />
			);
		}

		const handler = (e: React.FormEvent<HTMLInputElement>) => {
			this.props.setRouteProperty(param.Name, e.currentTarget.value);
		};

		return (
			<InputGroup id={param.Name} placeholder='Placeholder text' value={value} onChange={handler} fill/>
		);
	}

	private renderBooleanParam = (param: IValueDescriptor) => {
		const value = this.props.properties[param.Name];

		const handler = () => {
			this.props.setRouteProperty(param.Name, value === 'False' ? 'True' : 'False');
		};

		return (
			<Button id={param.Name} active={value === 'True'} onClick={handler} text={value} small />
		);
	}

	private activateRoute = () => {
		const { properties, route } = this.props;
		this.props.executeRoute(route, properties);
	}
}
