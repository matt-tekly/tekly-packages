import { IFramelineAppConfig, IFramelineEvent } from './FramelineTypes';

export class TimelineLayout {
	private rows: IFramelineEvent[] = [];

	public getRow(evt: IFramelineEvent, config: IFramelineAppConfig): number {

		const padding = config.minEventTime;

		for (let i = 0; i < this.rows.length; i++) {
			const rowEvt = this.rows[i];
			if ((rowEvt.EndTime + padding) < evt.StartTime) {
				this.rows[i] = evt;
				return i;
			}
		}

		this.rows.push(evt);
		return this.rows.length - 1;
	}

	public static getTotalRows(evts: IFramelineEvent[], config: IFramelineAppConfig): number {
		const layout = new TimelineLayout();

		evts.forEach(evt => layout.getRow(evt, config));

		return layout.rows.length;
	}
}
