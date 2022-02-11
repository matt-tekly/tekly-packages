export interface IComponentRendererProps {
	component: IComponent;
	setComponentEnabled: (instanceId: number, enabled: boolean) => void;
}

export interface IHierarchy {
	GameObjects: IGameObject[];
}

export interface IGameObject {
	Active: boolean;
	ActiveSelf: boolean;
	Components: IComponent[];
	Path: string;
	Name: string;
	InstanceId: number;
	Children: IGameObject[];
	IsScene: boolean;
	Layer: string;
}

export interface IComponent {
	InstanceId: number;
	Enabled: boolean;
	EnabledSelf: boolean;
	CanBeDisabled: boolean;
	Type: string;
	Values: object;
}

export interface ITransform {
	Position: IVector3;
	Rotation: IVector3;
	Scale: IVector3;
}

export interface IVector3 {
	x: number;
	y: number;
	z: number;
}
