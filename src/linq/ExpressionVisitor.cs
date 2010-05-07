using System;
using System.Linq.Expressions;

namespace Kiss.Linq
{
    ///<summary>
    /// Expression visitor 
    ///</summary>
    public class ExpressionVisitor
    {
        /// <summary>
        /// Visits expression and delegates call to different to branch.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public virtual Expression Visit ( Expression expression )
        {
            if ( expression == null )
                return null;

            switch ( expression.NodeType )
            {
                case ExpressionType.Lambda:
                    return VisitLamda ( ( LambdaExpression ) expression );
                case ExpressionType.ArrayLength:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.Negate:
                case ExpressionType.UnaryPlus:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                    return this.VisitUnary ( ( UnaryExpression ) expression );
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.ArrayIndex:
                case ExpressionType.Coalesce:
                case ExpressionType.Divide:
                case ExpressionType.Equal:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LeftShift:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.Modulo:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.NotEqual:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.Power:
                case ExpressionType.RightShift:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return this.VisitBinary ( ( BinaryExpression ) expression );
                case ExpressionType.Call:
                    return this.VisitMethodCall ( ( MethodCallExpression ) expression );
                case ExpressionType.Constant:
                    return this.VisitConstant ( ( ConstantExpression ) expression );

            }
            throw new ArgumentOutOfRangeException ( "expression", expression.NodeType.ToString ( ), Properties.Resource.UnknowNodeType );
        }

        public virtual Expression VisitConstant ( ConstantExpression expression )
        {
            return expression;
        }

        public virtual Expression VisitMethodCall ( MethodCallExpression expression )
        {
            throw new NotImplementedException ( );
        }

        public virtual Expression VisitBinary ( BinaryExpression expression )
        {
            this.Visit ( expression.Left );
            this.Visit ( expression.Right );
            return expression;
        }

        public virtual Expression VisitUnary ( UnaryExpression expression )
        {
            this.Visit ( expression.Operand );
            return expression;
        }

        public virtual Expression VisitLamda ( LambdaExpression lambdaExpression )
        {
            this.Visit ( lambdaExpression.Body );
            return lambdaExpression;
        }
    }
}
