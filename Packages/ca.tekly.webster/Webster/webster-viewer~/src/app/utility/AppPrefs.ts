
export function getBoolean(key: string, defaultValue?: boolean): boolean {
	const result = localStorage.getItem(key);
	if (result) {
		return result === 'true';
	}

	return !!defaultValue;
}

export function setBoolean(key: string, value: boolean): void {
	localStorage.setItem(key, value ? 'true' : 'false');
}
