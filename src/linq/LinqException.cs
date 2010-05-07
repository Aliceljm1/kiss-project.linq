#region File Comment
//+-------------------------------------------------------------------+
//+ File Created:   2009-09-03
//+-------------------------------------------------------------------+
//+ History:
//+-------------------------------------------------------------------+
//+ 2009-09-03		zhli Comment Created
//+-------------------------------------------------------------------+
#endregion

using System;

namespace Kiss.Linq
{
    public class LinqException : KissException
    {
        public LinqException ( string message, Exception ex ) : base ( message, ex ) { }
        public LinqException ( string message ) : base ( message ) { }
    }
}
