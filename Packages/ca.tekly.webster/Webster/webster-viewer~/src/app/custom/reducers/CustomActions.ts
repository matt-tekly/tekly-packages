export function setCustomThing(thing: string) {
	return {
		type: 'SET_CUSTOM_THING',
		payload: thing
	} as const;
}


export type CustomActions = ReturnType<
	typeof setCustomThing
>;
