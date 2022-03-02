using System;
using System.Collections.Generic;
using NCalc;

namespace Tekly.Common.Expressions
{
    public class ExpressionFactory
    {
        private readonly Dictionary<string, WeakReference<ExpressionContainer>> m_expressionContainers = new Dictionary<string, WeakReference<ExpressionContainer>>();
        private readonly ParameterExtractor m_parameterExtractor = new ParameterExtractor();
        
        public ExpressionContainer Create(string expression)
        {
            ExpressionContainer expressionContainer;

            if (m_expressionContainers.TryGetValue(expression, out var wr)) {
                if (wr.TryGetTarget(out expressionContainer)) {
                    return expressionContainer;
                }
            }

            var logicalExpression = Expression.Compile(expression, false);
            var parameters = m_parameterExtractor.Extract(logicalExpression);
            expressionContainer = new ExpressionContainer(logicalExpression, parameters, expression);
            
            m_expressionContainers[expression] = new WeakReference<ExpressionContainer>(expressionContainer);

            return expressionContainer;
        }
    }
}