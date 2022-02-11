import Log from './Log';

export type ResponseType = 'Json' | 'None' | 'Text';

export class Fetcher {
	
	private defaultHeaders = {
		'X-Requested-With': 'HMLHttpRequest',
		'cache-control': 'no-cache',
		'pragma': 'no-cache'
	};

	private urlPrefix = '';
	private log = Log('Fetcher', 'utility');

	constructor(urlPrefix: '') {
		this.urlPrefix = urlPrefix;
	}

	public async get(url: string, responseType: ResponseType = 'None') {
		return this.doFetchNoBody(url, 'get', responseType);
	}

	public async put(url: string, responseType: ResponseType = 'None') {
		return this.doFetchNoBody(url, 'put', responseType);
	}

	public async delete(url: string, responseType: ResponseType = 'None') {
		return this.doFetchNoBody(url, 'delete', responseType);
	}

	public async do(url: string, verb: string, responseType: ResponseType = 'None') {
		return this.doFetchNoBody(url, verb, responseType);
	}

	private async doFetchJsonBody(url: string, body: any, method: string, responseType: ResponseType) {
		const params = {
			body: JSON.stringify(body),
			headers: {
				...this.defaultHeaders,
				'Content-Type': 'application/json'
			},
			method
		};

		return this.doFetch(url, params, responseType);
	}

	private async doFetchNoBody(url: string, method: string, responseType: ResponseType) {
		const params = {
			headers: {
				...this.defaultHeaders,
			},
			// Hack to make WebSocketSharp work correctly with empty puts. Without a body it complains about Content-Length not being set.
			body: method.toLowerCase() !== 'get' ? 'empty' : undefined,
			method
		};

		return this.doFetch(url, params, responseType);
	}

	private async doFetch(url: string, params: RequestInit, responseType: ResponseType) {
		const response = await this.fetchWrapper(url, params);
		if (response.ok) {
			const body = await this.getResponse(response, responseType);

			return {
				response,
				body
			};
		}

		let errorMessage = `Error: ${response.status} - ${params.method} ${response.url}`;

		try {
			const errorBody = await response.json();
			if (errorBody && errorBody.message) {
				errorMessage += '\n' + errorBody.message;
			}
		} catch (err) {
			// do nothing
		}

		this.log.error(errorMessage);

		throw new Error(errorMessage);
	}

	private async fetchWrapper(url: string, params: RequestInit) {
		return fetch(this.urlPrefix + url, params);
	}

	private async getResponse(response: Response, responseType: ResponseType) {
		switch (responseType) {
			case 'Json': {
				try {
					return await response.json();
				} catch (err) {
					this.log.error(`Error converting response to json`, response);
					throw err;
				}
			}
			case 'Text': {
				try {
					return await response.text();
				} catch (err) {
					this.log.error(`Error converting response to text`, response);
					throw err;
				}
			}
			case 'None': {
				return null;
			}
			default: {
				throw new Error(`Unknown response type ${responseType}`);
			}
		}
	}
}

export const GameFetcher = new Fetcher('');
