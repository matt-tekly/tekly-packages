import * as React from 'react';

import { connect } from 'react-redux';

import { Tab, TabId, Tabs } from '@blueprintjs/core';
import { IAppState } from '~app/AppState';
import { ICellData, ITableHeading } from '~app/components/tables/Table';
import { refreshAssets } from '../reducers/AssetsActions';
import { IAssetsSummary, IAudioClipSummary, IMaterialSummary, IMeshSummary, IShaderSummary, ISpriteSummary, ITextureSummary } from '../Types';
import { AssetsSummary } from './AssetsSummary';
import { AssetSummaryTable } from './AssetSummaryTable';
import { TexturesSmmaryTable } from './TexturesSummaryTable';
import { SpritesSummaryTable } from './SpritesSummaryTable';

interface IProps {
	assets: IAssetsSummary;
	refreshAssets: () => void;
}

interface IState {
	selectedTab: TabId;
}

class AssetsView extends React.Component<IProps> {

	public state: IState = {
		selectedTab: 'Summary'
	};

	public async componentDidMount() {
		this.props.refreshAssets();
	}

	public render() {
		return (
			<div className='assets-view'>
				<Tabs onChange={this.handleNavbarTabChange} selectedTabId={this.state.selectedTab} vertical renderActiveTabPanelOnly large>
					<Tab id='Summary' title='Summary' />
					{this.renderTab('Audio Clips', this.props.assets.AudioClips.length)}
					{this.renderTab('Materials', this.props.assets.Materials.length)}
					{this.renderTab('Meshes', this.props.assets.Meshes.length)}
					{this.renderTab('Shaders', this.props.assets.Shaders.length)}
					{this.renderTab('Sprites', this.props.assets.Sprites.length)}
					{this.renderTab('Textures', this.props.assets.Textures.length)}
				</Tabs>
				{this.renderActiveTab()}
			</div>
		);
	}

	private renderTab(title: string, count: number) {
		return (
			<Tab id={title}>
				<span>{title}</span>
				<span style={{float: 'right'}}> {count.toString()}</span>
			</Tab>
		);
	}

	private handleNavbarTabChange = (selectedTab: TabId) => this.setState({ selectedTab });

	private renderActiveTab() {
		switch (this.state.selectedTab) {
			case 'Summary': return <AssetsSummary assets={this.props.assets} />;
			case 'Audio Clips': return <AssetSummaryTable assets={this.props.assets.AudioClips} getTableData={this.getAudioClipsTableData} />;
			case 'Materials': return <AssetSummaryTable assets={this.props.assets.Materials} getTableData={this.getMaterialsTableData} />;
			case 'Meshes': return <AssetSummaryTable assets={this.props.assets.Meshes} getTableData={this.getMeshesTableData} />;
			case 'Shaders': return <AssetSummaryTable assets={this.props.assets.Shaders} getTableData={this.getShaderTableData} />;
			case 'Sprites': return <SpritesSummaryTable assets={this.props.assets.Sprites} getTableData={this.getSpritesTableData} />;
			case 'Textures': return <TexturesSmmaryTable assets={this.props.assets.Textures} getTableData={this.getTexturesTableData} />;
		}

		return <AssetsSummary assets={this.props.assets} />;
	}

	private getAudioClipsTableData = (assets: IAudioClipSummary[]) => {
		const headings: ITableHeading[] = [
			{ text: 'Name', sortable: true },
			{ text: 'Seconds', sortable: true }
		];

		const rows: ICellData[][] = assets.map(x => {
			return [
				{ text: x.Name, sortValue: x.Name },
				{ text: x.Length.toFixed(2), sortValue: x.Length }
			];
		});

		return { headings, rows };
	}

	private getMaterialsTableData = (assets: IMaterialSummary[]) => {
		const headings: ITableHeading[] = [
			{ text: 'Name', sortable: true },
			{ text: 'Shader', sortable: true }
		];

		const rows: ICellData[][] = assets.map(x => {
			return [
				{ text: x.Name, sortValue: x.Name },
				{ text: x.Shader, sortValue: x.Shader }
			];
		});

		return { headings, rows };
	}

	private getMeshesTableData = (assets: IMeshSummary[]) => {
		const headings: ITableHeading[] = [
			{ text: 'Name', sortable: true },
			{ text: 'Sub Mesh Count', sortable: true },
			{ text: 'Vertex Count', sortable: true },
			{ text: 'Triangles', sortable: true },
		];

		const rows: ICellData[][] = assets.map(x => {
			return [
				{ text: x.Name, sortValue: x.Name },
				{ text: x.SubMeshCount.toString(), sortValue: x.SubMeshCount },
				{ text: x.VertexCount.toString(), sortValue: x.VertexCount },
				{ text: x.Triangles.toString(), sortValue: x.Triangles }
			];
		});

		return { headings, rows };
	}

	private getShaderTableData = (assets: IShaderSummary[]) => {
		const headings: ITableHeading[] = [
			{ text: 'Name', sortable: true },
		];

		const rows: ICellData[][] = assets.map(x => {
			return [
				{ text: x.Name, sortValue: x.Name },
			];
		});

		return { headings, rows };
	}

	private getTexturesTableData = (assets: ITextureSummary[]) => {
		const headings: ITableHeading[] = [
			{ text: 'Name', sortable: true },
			{ text: 'Width', sortable: true },
			{ text: 'Height', sortable: true },
			{ text: 'Total Pixels', sortable: true },
		];

		const rows: ICellData[][] = assets.map(x => {
			return [
				{ text: x.Name, sortValue: x.Name, userData: x },
				{ text: x.Width.toString(), sortValue: x.Width, userData: x },
				{ text: x.Height.toString(), sortValue: x.Height, userData: x },
				{ text: (x.Height * x.Width).toString(), sortValue: (x.Height * x.Width), userData: x },
			];
		});

		return { headings, rows };
	}

	private getSpritesTableData = (assets: ISpriteSummary[]) => {
		const headings: ITableHeading[] = [
			{ text: 'Name', sortable: true },
			{ text: 'Width', sortable: true },
			{ text: 'Height', sortable: true },
			{ text: 'Total Pixels', sortable: true },
			{ text: 'In Atlas', sortable: true },
			{ text: 'Source', sortable: true },
		];

		const rows: ICellData[][] = assets.map(x => {
			return [
				{ text: x.Name, sortValue: x.Name, userData: x },
				{ text: x.Width.toString(), sortValue: x.Width, userData: x },
				{ text: x.Height.toString(), sortValue: x.Height, userData: x },
				{ text: (x.Height * x.Width).toString(), sortValue: (x.Height * x.Width), userData: x },
				{ text: x.InAtlas ? 'Atlased' : 'Single', sortValue: x.Name, userData: x },
				{ text: x.TextureInstanceId.toString(), sortValue: x.Name, userData: x },
			];
		});

		return { headings, rows };
	}
}

const mapStateToProps = (state: IAppState) => {
	return {
		assets: state.assets.assets
	};
};

export default connect(mapStateToProps, { refreshAssets })(AssetsView);

