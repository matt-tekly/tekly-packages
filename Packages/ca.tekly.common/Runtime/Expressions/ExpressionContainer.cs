using NCalc.Domain;

namespace Tekly.Common.Expressions
{
    public class ExpressionContainer
    {
        public readonly LogicalExpression LogicalExpression;
        public readonly string[] Parameters;
        public readonly string SourceExpression;
        
        public ExpressionContainer(LogicalExpression logicalExpression, string[] parameters, string sourceExpression)
        {
            LogicalExpression = logicalExpression;
            Parameters = parameters;
            SourceExpression = sourceExpression;
        }
    }
}