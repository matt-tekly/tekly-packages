using System.Text;

namespace Tekly.Sheets.Data
{
    public enum PrintMode
    {
        Pretty,
        Condensed
    }

    /// <summary>
    ///     Provides an easy to use API for generating complicated strings like JSON.
    /// </summary>
    public class StringBlock
    {
        private enum Mode
        {
            LineStart,
            LineMid
        }

        private readonly StringBuilder m_text = new StringBuilder(1024);
        private readonly PrintMode m_printMode;

        private string m_indentText;
        private int m_indentLevel;
        private Mode m_mode;

        public StringBlock(PrintMode indentMode = PrintMode.Pretty)
        {
            m_printMode = indentMode;
            m_indentText = "";
            m_indentLevel = 0;

            m_mode = Mode.LineStart;
        }
        
        private void AppendIndent()
        {
            if (m_printMode == PrintMode.Pretty) {
                m_text.Append(m_indentText);
            }
        }

        public override string ToString()
        {
            return m_text.ToString();
        }

        public void Append(string text)
        {
            if (m_mode == Mode.LineStart) {
                AppendIndent();
            }

            m_text.Append(text);
            m_mode = Mode.LineMid;
        }

        public void AppendLine(string text)
        {
            if (m_mode == Mode.LineStart) {
                AppendIndent();
            }

            if (m_printMode == PrintMode.Pretty) {
                m_text.AppendLine(text);    
            } else {
                m_text.Append(text);
            }
            
            m_mode = Mode.LineStart;
        }

        public void NewLine()
        {
            if (m_printMode == PrintMode.Pretty) {
                m_text.AppendLine();
                m_mode = Mode.LineStart;
            }
        }

        public void OpenBlock(string text)
        {
            AppendLine(text);
            InBrace();
        }

        public void CloseBlock()
        {
            OutBrace();
        }

        public void InBrace()
        {
            AppendLine("{");
            Indent();
        }

        public void OutBrace()
        {
            Outdent();
            NewLine();
            Append("}");
        }

        public void InBracket()
        {
            AppendLine("[");
            Indent();
        }

        public void OutBracket()
        {
            Outdent();
            NewLine();
            Append("]");
        }

        public void Indent()
        {
            SetIndent(m_indentLevel + 1);
        }

        public void Outdent()
        {
            SetIndent(m_indentLevel - 1);
        }

        public void SetIndent(int level)
        {
            m_indentLevel = level;
            if (m_printMode == PrintMode.Pretty) {
                m_indentText = new string('\t', m_indentLevel);
            }
        }
    }
}