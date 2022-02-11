import { IDictionary } from "~app/utility/Types";

export interface IRouteInfo {
	Routes: IRouteDescriptor[];
}

export interface IRouteDescriptor {
	Path: string;
	Description: string;
	Verb: string;
	ReturnType: string;
	Hidden: boolean;
	QueryParams: IValueDescriptor[];
}

export interface IValueDescriptor {
	Name: string;
	Type: string;
	Values: string[];
	Description: string;
	Optional: boolean;
	DefaultValue: string;
}

export interface IRouteResponse {
	statusText: string;
	result: any;
}

export interface IRouteState {
	values: IDictionary<any>;	
}