export interface IDictionary<T> {
	[key: string]: T;
}

export interface IInfoSummary {
	Info: IInfoItem[]
}

export interface IInfoItem {
	Name: string;
	Value: string;
	Category: string;
}