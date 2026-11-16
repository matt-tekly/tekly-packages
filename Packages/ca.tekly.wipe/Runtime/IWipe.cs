namespace Tekly.Wipes
{
    /// <summary>
    /// Something a wipe track drives: a Wipe or a Wipe Reveal.
    /// </summary>
    public interface IWipe
    {
        /// <summary>
        /// How much is covered: 0 is none and 1 is all. Cover raises it and Uncover lowers it.
        /// </summary>
        float Coverage { get; set; }

        /// <summary>
        /// True while an Uncover carries the wipe on through, so it uncovers in the order it covered instead
        /// of going back the way it came.
        /// </summary>
        bool Leaving { get; set; }

        /// <summary>
        /// Sets the coverage and whether the wipe is leaving in one go.
        /// </summary>
        void Set(float coverage, bool leaving);

        /// <summary>
        /// The style a look draws with here: the look's, or this wipe's own when the look is null.
        /// </summary>
        WipeStyle StyleFor(WipeLook look);

        /// <summary>
        /// Called by a track every time it evaluates, before the coverage changes. The look is the clip's,
        /// or null for this wipe's own. Moving is true while a clip is part way through, even if an ease
        /// has overshot to full coverage.
        /// </summary>
        void SetTrackState(WipeLook look, bool moving);
    }
}
