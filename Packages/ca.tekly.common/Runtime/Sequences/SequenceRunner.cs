using System.Collections.Generic;

namespace Tekly.Common.Sequences
{
    public class SequenceRunner
    {
        private readonly List<Sequence> m_sequences = new List<Sequence>();

        public bool AnyActive => m_sequences.Count > 0;
        
        public void Add(Sequence sequence)
        {
            m_sequences.Add(sequence);
        }

        public void Tick(float deltaTime)
        {
            foreach (var sequence in m_sequences) {
                sequence.Update(deltaTime);
            }
            
            m_sequences.RemoveAll(x => x.Completed);
        }
    }
}