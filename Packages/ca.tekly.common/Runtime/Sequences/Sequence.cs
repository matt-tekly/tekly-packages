using System;
using System.Linq;
using UnityEngine;

namespace Tekly.Common.Sequences
{
    public class Sequence
    {
        public bool Completed { get; private set; }

        public void Update()
        {
            if (Completed) {
                return;
            }

            Completed = Animate();
        }

        protected virtual bool Animate()
        {
            return true;
        }
    }

    public class ParallelSequence : Sequence
    {
        private readonly Sequence[] m_sequences;
        
        public ParallelSequence(params Sequence[] sequence)
        {
            m_sequences = sequence;
        }
        
        protected override bool Animate()
        {
            foreach (var sequence in m_sequences) {
                sequence.Update();
            }

            return m_sequences.All(x => x.Completed);
        }
    }
    
    public class SequentialSequence : Sequence
    {
        private int m_index;
        private readonly Sequence[] m_sequences;
        
        public SequentialSequence(params Sequence[] sequence)
        {
            m_sequences = sequence;
        }
        
        protected override bool Animate()
        {
            m_sequences[m_index].Update();
            
            if (m_sequences[m_index].Completed) {
                m_index++;
            }

            return m_index == m_sequences.Length;
        }
    }
    
    public class ActionSequence : Sequence
    {
        private readonly Action m_action;
        
        public ActionSequence(Action action)
        {
            m_action = action;
        }

        protected override bool Animate()
        {
            m_action();
            return true;
        }
    }
    
    public class DelaySequence : Sequence
    {
        private float m_timer;
        
        public DelaySequence(float delay)
        {
            m_timer = delay;
        }

        protected override bool Animate()
        {
            m_timer -= Time.deltaTime;
            return m_timer <= 0;
        }
    }
    
    public class DelayActionSequence : Sequence
    {
        private float m_timer;
        private readonly Action m_action;

        public DelayActionSequence(float delay, Action action)
        {
            m_timer = delay;
            m_action = action;
        }

        protected override bool Animate()
        {
            m_timer -= Time.deltaTime;

            if (m_timer <= 0) {
                m_action();
            }

            return m_timer <= 0;
        }
    }
}