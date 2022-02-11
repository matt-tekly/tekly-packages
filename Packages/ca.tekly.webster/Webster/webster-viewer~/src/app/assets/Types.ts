
export type AssetSummaryType = keyof IAssetsSummary;

export const AssetSummaryTypes: AssetSummaryType[] = [
	'AudioClips',
	'Materials',
	'Meshes',
	'Shaders',
	'Sprites',
	'Textures'
];

export interface IAssetsSummary {
	AudioClips: IAudioClipSummary[];
	Materials: IMaterialSummary[];
	Meshes: IMeshSummary[];
	Shaders: IShaderSummary[];
	Sprites: ISpriteSummary[];
	Textures: ITextureSummary[];
}

export interface IAssetSummary {
	Name: string;
	Type: string;
	InstanceId: number;
}

export interface IAudioClipSummary extends IAssetSummary  {
	Length: number;
}

export interface IMaterialSummary extends IAssetSummary  {
	Shader: string;
}

export interface IMeshSummary extends IAssetSummary  {
	Triangles: number;
	SubMeshCount: number;
	VertexCount: number;
}

export interface IShaderSummary extends IAssetSummary  {

}

export interface ISpriteSummary extends IAssetSummary {
	Width: number;
	Height: number;
	InAtlas: boolean;

	TextureInstanceId: number;
}

export interface ITextureSummary extends IAssetSummary {
	Width: number;
	Height: number;
	TextureFormat: string;
}
