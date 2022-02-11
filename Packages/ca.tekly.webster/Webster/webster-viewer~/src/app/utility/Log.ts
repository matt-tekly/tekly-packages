// tslint:disable: no-console

export class Log {

	private file: string;

	public constructor(file: string) {
		this.file = file;
	}

	public info(message?: any, ...optionalParams: any[]): void {
		console.info(message, ...optionalParams);
	}

	public error(message?: any, ...optionalParams: any[]): void {
		console.error(message, optionalParams);
	}
}

export default function (file: string, directory: string) {
	return new Log(directory + '/' + file);
}
