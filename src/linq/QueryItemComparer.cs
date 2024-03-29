﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace Kiss.Linq
{
    public class QueryItemComparer<T> : IComparer<T> where T : QueryObject
    {
        public QueryItemComparer(string orderByField, bool asc)
        {
            this.orderByField = orderByField;
            ascending = asc;
        }

        private readonly string orderByField = string.Empty;
        private readonly bool ascending = true;

        int IComparer<T>.Compare(T x, T y)
        {
            PropertyInfo prop1 = x.ReferringObject.GetType().GetProperty(orderByField, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            PropertyInfo prop2 = y.ReferringObject.GetType().GetProperty(orderByField, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
          
            if (prop1 != null && prop2 != null)
            {
                int result = 0;

                object obj1 = prop1.GetValue(x.ReferringObject, null);
                object obj2 = prop2.GetValue(y.ReferringObject, null);

                if (ascending)
                {
                    result = GetComparisonResult(obj1, obj2);
                }
                else
                {
                    result = GetComparisonResult(obj2, obj1);
                }

               return result;
            }
            return 0;
        }

        private static int GetComparisonResult(object obj1, object obj2)
        {
            int result = 0;
            string type = obj1.GetType().FullName;

            switch (type)
            {
                case "System.DateTime":
                    result = ((DateTime)obj1).CompareTo((DateTime)obj2);
                    break;
                case "System.String":
                    result = ((String)obj1).CompareTo((String)obj2);
                    break;
                case "System.Int32":
                    result = ((int)obj1).CompareTo((int)obj2);
                    break;
                case "System.Double":
                    result = ((double)obj1).CompareTo((double)obj2);
                    break;
                default:
                    result = ((string)obj1).CompareTo((string)obj2);
                    break;
            }
            return result;
        }


    }
}
