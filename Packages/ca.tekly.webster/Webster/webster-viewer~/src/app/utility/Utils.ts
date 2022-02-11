export function distinct<T>(value: T, index: number, self: T[]) {
	return self.indexOf(value) === index;
}

export function groupBy<T, Q>(arr: Array<T>, func: (val: T) => Q) {
	return arr.reduce(function (acc: Map<Q, T[]>, obj: T) {
		let key: Q = func(obj);
		let vals = acc.get(key);
		if (!vals) {
			vals = [];
			acc.set(key, vals);
		} 
		vals.push(obj);
		return acc;
	}, new Map<Q, T[]>());
}

export function mapMap<K, V, OUT>(map: Map<K, V>, func: (key: K, value: V) => OUT): OUT[] {
	const out: OUT[] = [];
	map.forEach((value, key) => {
		out.push(func(key, value));
	});

	return out;
}