using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tekly.BuildSequences.Editor
{
    [CreateAssetMenu(menuName = "Tekly/Build Sequences/Sequencer")]
    public class BuildSequencer : ScriptableObject
    {
        public bool Development = true;
        public BuildSequence[] Sequences;

        public void Build(BuildSequenceParams buildSequenceParams)
        {
            BuildSequencerInstance instance = new BuildSequencerInstance(this, buildSequenceParams);
            instance.Build();
        }
    }

    [CustomEditor(typeof(BuildSequencer))]
    public class BuildSequencerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Build")) {
                var sequencer = (BuildSequencer) target;
                sequencer.Build(new BuildSequenceParams {
                    BuildTarget = BuildTarget.StandaloneWindows
                });
            }
        }
    }

    public class BuildSequenceParams
    {
        public bool Development;
        public BuildTarget BuildTarget;
    }

    public class BuildSequenceContext
    {
        public BuildOptions BuildOptions = BuildOptions.None;
        public BuildTarget BuildTarget;
        public string AppFileName;
        public string LocationPath = Path.GetFullPath("Builds/");
        public List<string> Scenes = new List<string>();
        public List<string> Defines = new List<string>();

        public BuildPlayerOptions ToBuildPlayerOptions()
        {
            return new BuildPlayerOptions {
                options = BuildOptions,
                target = BuildTarget,
                targetGroup = BuildPipeline.GetBuildTargetGroup(BuildTarget),
                scenes = Scenes.ToArray(),
                extraScriptingDefines = Defines.ToArray(),
                locationPathName = Path.Combine(LocationPath, AppFileName)
            };
        }
    }

    public class BuildSequencerInstance
    {
        private readonly BuildSequenceParams m_buildSequenceParams;
        private readonly BuildSequencer m_sequencer;
        private readonly List<BuildSequence> m_sequences;
        private readonly BuildSequenceContext m_context;

        public BuildSequencerInstance(BuildSequencer sequencer, BuildSequenceParams buildSequenceParams)
        {
            m_buildSequenceParams = buildSequenceParams;

            var buildOptions = BuildOptions.DetailedBuildReport;

            if (sequencer.Development) {
                buildOptions |= BuildOptions.Development;
            }

            m_context = new BuildSequenceContext {
                BuildTarget = buildSequenceParams.BuildTarget,
                BuildOptions = buildOptions
            };

            m_sequencer = Object.Instantiate(sequencer);
            m_sequences = m_sequencer.Sequences
                .Where(x => x.Condition.IsActive(m_context))
                .Select(Object.Instantiate)
                .ToList();
        }

        public void Build()
        {
            Debug.Log($"Build Sequence [{m_sequencer.name}] Started");

            foreach (var buildSequence in m_sequences) {
                buildSequence.PreBuild(m_context);
            }

            var buildOptions = m_context.ToBuildPlayerOptions();
            Debug.Log($"BuildPath: {buildOptions.locationPathName}");

            var report = BuildPipeline.BuildPlayer(buildOptions);

            foreach (var buildSequence in m_sequences) {
                buildSequence.PostBuild(m_context, report);
            }

            Debug.Log($"Build Sequence [{m_sequencer.name}] Finished");
        }
    }
}