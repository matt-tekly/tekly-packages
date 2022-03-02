using System.Collections.Generic;
using System.Linq;
using NCalc.Domain;
using UnityEngine;

namespace Tekly.Common.Expressions
{
    public class ParameterExtractor : LogicalExpressionVisitor
    {
        public readonly HashSet<string> Parameters = new HashSet<string>();

        public string[] Extract(LogicalExpression logicalExpression)
        {
            logicalExpression.Accept(this);

            var result = Parameters.ToArray();

            Parameters.Clear();

            return result;
        }

        public override void Visit(Identifier identifier)
        {
            Parameters.Add(identifier.Name);
        }

        public override void Visit(UnaryExpression expression)
        {
            expression.Expression.Accept(this);
        }

        public override void Visit(BinaryExpression expression)
        {
            expression.LeftExpression.Accept(this);
            expression.RightExpression.Accept(this);
        }

        public override void Visit(TernaryExpression expression)
        {
            expression.LeftExpression.Accept(this);
            expression.RightExpression.Accept(this);
            expression.MiddleExpression.Accept(this);
        }

        public override void Visit(Function function)
        {
            foreach (var expression in function.Expressions) {
                expression.Accept(this);
            }
        }

        public override void Visit(LogicalExpression expression) { }

        public override void Visit(ValueExpression expression) { }
    }
}