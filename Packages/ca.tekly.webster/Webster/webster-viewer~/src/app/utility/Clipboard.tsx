
export async function getClipboardObject(): Promise<object> {
	const content = await navigator.clipboard.readText();

	try {
		return JSON.parse(content);
	} catch (err) {
		console.error(`Failed to parse JSON from clipboard\n${err}`);
		return {};
	}
}
