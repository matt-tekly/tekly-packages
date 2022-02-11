using System;
using System.Collections.Generic;

namespace Tekly.Common.Sequences
{
    public static class SequenceBuilder
    {
        public static Sequence Parallel(Action<SequenceBuilderImpl> builder)
        {
            var sb = new SequenceBuilderImpl();
            builder.Invoke(sb);
            
            return sb.ToParallel();
        }
        
        public static Sequence Sequential(Action<SequenceBuilderImpl> builder)
        {
            var sb = new SequenceBuilderImpl();
            builder.Invoke(sb);
            
            return sb.ToSequential();
        }
        
        public static readonly Sequence Completed = new Sequence();
    }
    
    public class SequenceBuilderImpl
    {
        private readonly List<Sequence> m_sequences = new List<Sequence>();
        
        public void Add(Sequence sequence)
        {
            m_sequences.Add(sequence);
        }
        
        public void Action(Action action)
        {
            m_sequences.Add(new ActionSequence(action));
        }
        
        public void Delay(float delay)
        {
            Add(new DelaySequence(delay));
        }

        public void DelayAction(float delay, Action action)
        {
            Add(new DelayActionSequence(delay, action));
        }
        
        public void Sequential(Action<SequenceBuilderImpl> builder)
        {
            var sb = new SequenceBuilderImpl();
            builder(sb);
            Add(sb.ToSequential());
        }
        
        public void Parallel(Action<SequenceBuilderImpl> builder)
        {
            var sb = new SequenceBuilderImpl();
            builder(sb);
            Add(sb.ToParallel());
        }
        
        public Sequence ToSequential()
        {
            return new SequentialSequence(m_sequences.ToArray());
        }
        
        public Sequence ToParallel()
        {
            return new ParallelSequence(m_sequences.ToArray());
        }
    }
}