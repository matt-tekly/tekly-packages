using Tekly.Common.Collections;

namespace Tekly.Common.Sequences
{
    public class SequenceRunner
    {
        private readonly SafeList<Sequence> m_sequences = new SafeList<Sequence>();

        public bool AnyActive => m_sequences.Count > 0;
        
        public void Add(Sequence sequence)
        {
            m_sequences.Add(sequence);
        }

        public void Tick()
        {
            foreach (var sequence in m_sequences) {
                sequence.Update();
            }
            
            m_sequences.RemoveAll(x => x.Completed);
        }
    }
}