export interface IDirectoryResponse {
	PersistentDataPath: string;
	Directory: IDirectorySummary;
}

export interface IDiskEntry {
	Name: string;
	Path: string;
}

export interface IDirectorySummary extends IDiskEntry {
	Directories: IDirectorySummary[];
	Files: IFileSummary[];
	Type: 'Directory';
}

export interface IFileSummary extends IDiskEntry {
	Size: number;
	CreationTime: string;
	LastWriteTime: string;
	Type: 'File';
}

export type IDiskEntrySummary = IDirectorySummary | IFileSummary;
