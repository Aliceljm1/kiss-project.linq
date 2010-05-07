using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Kiss.Linq
{
    public static class QueryExtension
    {
        private static readonly IDictionary<string, IDictionary<string, object>> uniqueDefaultValueObjectMap =
            new Dictionary<string, IDictionary<string, object>>();

        internal static IDictionary<string, object> GetUniqueItemDefaultDetail(this IQueryObject obj)
        {
            Type runningType = obj.GetType();
           
            if (!uniqueDefaultValueObjectMap.ContainsKey(runningType.Name))
            {
                IDictionary<string, object> uniqueDefaultValues = new Dictionary<string, object>();
                //clone the result.
                object runningObject = Activator.CreateInstance(runningType);

                PropertyInfo[] infos = runningType.GetProperties();
                
                int index = 0;

                foreach (PropertyInfo info in infos)
                {
                    object[] arg = info.GetCustomAttributes(typeof (UniqueIdentifierAttribute), true);

                    if (arg != null && arg.Length > 0)
                    {
                        object value = info.GetValue(runningObject, null);
                       
                        if (!uniqueDefaultValues.ContainsKey(info.Name))
                        {
                            uniqueDefaultValues.Add(info.Name, new { Index = index, Value = value });
                        }
                    }
                    index++;
                }
                uniqueDefaultValueObjectMap.Add(runningType.Name, uniqueDefaultValues);
            }
            return uniqueDefaultValueObjectMap[runningType.Name];
        }

        internal static T Cast<T>(object obj, T type)
        {
            return (T)obj;
        }

        public static UnaryExpression GetUnaryExpressionFromMethodCall(Expression expression)
        {
            MethodCallExpression mCall = expression as MethodCallExpression;

            UnaryExpression uExp = null;

            foreach (Expression exp in mCall.Arguments)
            {
                if (exp is UnaryExpression)
                {
                    uExp = exp as UnaryExpression;
                    break;
                }
                else if (exp is MethodCallExpression)
                {
                    uExp = GetUnaryExpressionFromMethodCall(exp);
                    break;
                }
            }
            return uExp;
        }
       
        /// <summary>
        /// tries to combine the values for a give a type . Ex User defined clasee
        /// and its properties.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object Combine(this IList<BucketItem.QueryCondition> list, Type type)
        {
            object combinedObject = Activator.CreateInstance(type);

            int index = 0;

            foreach (var condition in list)
            {
                condition.Value.CopyRecursive(combinedObject);
                index ++;
            }
            return combinedObject;
        }

        /// <summary>
        /// recursively copies object properties to destination.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        public static void CopyRecursive(this object source , object destination)
        {
            PropertyInfo[] sourceProperties = source.GetType().GetProperties();

            Type destType = destination.GetType();

            foreach (PropertyInfo prop in sourceProperties)
            {
                    
                if (prop.PropertyType.IsClass && !(prop.PropertyType.FullName.IndexOf("System") >= 0))
                {
                    object value = prop.GetValue(source, null);

                    PropertyInfo destProp = destType.GetProperty(prop.Name);

                    object destValue = destProp.GetValue(destination, null);

                    if (value != null)
                    {
                        try
                        {
                            if (destValue == null)
                            {
                                destValue = Activator.CreateInstance(destProp.PropertyType);
                            }
                            /// copy
                            value.CopyRecursive(destValue);

                            if (destProp.CanWrite)
                            {
                                destProp.SetValue(destination, destValue, null);
                            }
                        }
                        catch
                        {
                            /// skip
                        }
                    }
                }
                else
                {
                    bool isDefault = false;

                    object destValue = destType.GetProperty(prop.Name).GetValue(destination, null);

                    if (destValue != null)
                    {
                        object tempObject = Activator.CreateInstance(destType);
                        object tempValue = tempObject.GetType().GetProperty(prop.Name).GetValue(tempObject, null);
                        isDefault = tempValue.Equals(destValue);
                    }

                    if (destValue == null || isDefault)
                    {
                        destType.GetProperty(prop.Name).SetValue(destination, prop.GetValue(source, null), null);
                    }
                }
            }
        }

        private static IDictionary<string, object > _queryClases = new Dictionary<string, object>();

        public static IQueryProvider GetQueryClass<T>(this T objectBase) where T : IQueryProvider
        {
            string key = objectBase.GetType().FullName;

            if (!_queryClases.ContainsKey(key))
            {
                _queryClases.Add(key, objectBase);
            }
            return _queryClases[key] as IQueryProvider;
        }

        public static object GetValueFromExpression(this Expression expression)
        {
            object value = null;

            UnaryExpression unaryExpression = GetUnaryExpressionFromMethodCall(expression);
            LambdaExpression lambdaExpression = unaryExpression.Operand as LambdaExpression;
            
            // get the value by dynamic invocation, used for getting value for MemberType expression.
            value = Expression.Lambda(lambdaExpression.Body).Compile().DynamicInvoke();
            return value;
        }
        
        public static object InvokeMethod(string methodName, Type itemType, object obj)
        {
            MemberInfo[] memInfos = itemType.GetMembers();

            MethodInfo[] mInfos = itemType.GetMethods();
            foreach (MethodInfo mInfo in mInfos)
            {
                if (string.Compare(methodName, mInfo.Name, false) == 0)
                {
                    return itemType.InvokeMember(methodName, BindingFlags.InvokeMethod, null, obj, null);
                }
            }
            return null;
        }

        private static object GetAttribute(Type type, ICustomAttributeProvider info)
        {
            object[] arg = info.GetCustomAttributes(type, true);

            if (arg != null && arg.Length > 0)
            {
                return arg[0];
            }
            return null;
        }

        internal static bool IsEqual(object obj1, object obj2)
        {
            bool equal = true;

            PropertyInfo[] infos = obj1.GetType().GetProperties();

            foreach (PropertyInfo info in infos)
            {
                IgnoreAttribute queryAttribute = GetAttribute(typeof(IgnoreAttribute), info) as IgnoreAttribute;

                if (queryAttribute == null)
                {
                    object value1 = info.GetValue(obj1, null);
                    object value2 = info.GetValue(obj2, null);

                    if (Convert.ToString(value2) != Convert.ToString(value1))
                    {
                        equal = false;
                    }
                }
            }
            return equal;
        }


        public static string GetPropertyName(PropertyInfo info)
        {
            string fieldName = string.Empty;

            object[] arg = info.GetCustomAttributes(typeof(OriginalFieldNameAttribute), true);

            if (arg != null && arg.Length > 0)
            {
                var fieldNameAttr = arg[0] as OriginalFieldNameAttribute;
                
                if (fieldNameAttr != null) 
                    fieldName = fieldNameAttr.FieldName;
            }
            else
            {
                fieldName = info.Name;

            }
            return fieldName;
        }

        /// <summary>
        /// Gets <see cref="MemberInfo"/> from <see cref="Expression{TDelegate}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        /// <returns><see cref="MemberInfo"/></returns>
        internal static MemberInfo GetMemberFromExpression<T> ( this Expression<Func<T, object>> expression )
        {
            if ( expression.Body is MemberExpression )
            {
                MemberExpression memberExpression = ( MemberExpression ) expression.Body;
                return memberExpression.Member;
            }
            else
            {
                UnaryExpression unaryExpression = ( UnaryExpression ) expression.Body;

                if ( unaryExpression.Operand is MemberExpression )
                {
                    MemberExpression memberExpression = ( MemberExpression ) unaryExpression.Operand;
                    return memberExpression.Member;
                }
            }
            return null;
        }

        internal static bool EqualsDefault ( this object targetValue, string propertyName, object source )
        {
            if ( targetValue != null && ( targetValue.GetType ( ).IsPrimitive || targetValue.GetType ( ).IsEnum ) )
            {
                object @default = Activator.CreateInstance ( source.GetType ( ) );
                return @default.GetType ( ).GetProperty ( propertyName ).GetValue ( @default, null ).Equals ( targetValue );
            }
            return false;
        }
    }
}
